using System;
using System.Collections.Generic;
using System.Linq;
using JitJotData;
using WebMatrix.WebData;
using Language = dotcode.lib.common.Language;

namespace dotcode.Models
{
    public static class DbModel
    {
        public static bool CodeUnitExistsById(Guid id)
        {
            var dotCodeDb = new dotcodedbEntities();
            return dotCodeDb.CodeUnits.Any(c => c.Id == id);
        }

        public static bool CodeUnitExistsByAutoId(long autoid)
        {
            var dotCodeDb = new dotcodedbEntities();
            return dotCodeDb.CodeUnits.Any(c => c.AutoId == autoid);
        }

        public static CodeUnit GetCodeUnitByAutoId(long autoid, long version, dotcodedbEntities dotcodedb = null)
        {
            var dotCodeDb = dotcodedb ?? new dotcodedbEntities();
            return dotCodeDb.CodeUnits.SingleOrDefault(c => c.AutoId == autoid && c.VersionId == version);
        }

        public static CodeUnit GetCodeUnitById(Guid id)
        {
            var dotCodeDb = new dotcodedbEntities();
            return dotCodeDb.CodeUnits.SingleOrDefault(c => c.Id == id);
        }

        public static CodeUnit CreateCodeUnit(string source, string title, string description, IEnumerable<Guid> references)
        {
            var dotCodeDb = new dotcodedbEntities();
            var dbCodeUnit = new CodeUnit();
            var autoId = new CodeUnitAutoId();
            
            dbCodeUnit.CreatedOn = DateTime.UtcNow;
            dbCodeUnit.LanguageId = lib.common.Language.CSharp5.Id;
            dbCodeUnit.Id = Guid.NewGuid();
            dbCodeUnit.Source = source;
            dbCodeUnit.Title = title;
            dbCodeUnit.Description = description;
            dbCodeUnit.ModifiedOn = DateTime.UtcNow;
            dbCodeUnit.CodeUnitAutoId = autoId;

            SetCodeUnitUserId(dbCodeUnit);

            autoId = dotCodeDb.CodeUnitAutoIds.Add(autoId);
            dbCodeUnit = dotCodeDb.CodeUnits.Add(dbCodeUnit);
            dotCodeDb.SaveChanges();

            ReferenceModel.SetProjectReferences(dbCodeUnit.Id, references);

            return dbCodeUnit;
        }

        public static CodeUnit UpdateCodeUnit(Guid id, string source, string title, string description, bool ispublic, IEnumerable<Guid> references)
        {
            if (!ValidationModel.CanUserEditOrSaveProject(id))
            {
                return new CodeUnit();
            }

            var dotCodeDb = new dotcodedbEntities();
            var dbCodeUnit = dotCodeDb.CodeUnits.SingleOrDefault(c => c.Id == id);

            if (dbCodeUnit != null && dbCodeUnit.UserId == WebSecurity.CurrentUserId)
            {
                var newCodeUnit = new CodeUnit()
                {
                    Source = source,
                    ModifiedOn = DateTime.UtcNow,
                    CreatedOn = DateTime.UtcNow,
                    Title = title,
                    Description = description,
                    Id = Guid.NewGuid(),
                    UserId = WebSecurity.CurrentUserId,
                    IsPublic = ispublic,
                    LanguageId = dbCodeUnit.LanguageId,
                    ParentId = dbCodeUnit.Id,
                    VersionId = dotCodeDb.CodeUnits.SqlQuery("SELECT TOP 1 * FROM dbo.CodeUnits WHERE AutoId = {0} ORDER BY VersionId DESC", dbCodeUnit.AutoId).First().VersionId + 1,
                    AutoId = dbCodeUnit.AutoId
                };
                
                dotCodeDb.CodeUnits.Add(newCodeUnit);
                dotCodeDb.SaveChanges();
                ReferenceModel.SetProjectReferences(newCodeUnit.Id, references);
                return newCodeUnit;
            }

            return dbCodeUnit;
        }

        public static CodeUnit CloneCodeUnit(Guid id, string source, IEnumerable<Guid> references)
        {
            var dotCodeDb = new dotcodedbEntities();
            var clonedProject = CloneProject(id, source, dotCodeDb);
            
            if (clonedProject.Id == Guid.Empty)
                throw new ArgumentException("codeUnitId");

            ReferenceModel.SetProjectReferences(clonedProject.Id, references);

            return clonedProject;
        }

        private static CodeUnit CloneProject(Guid codeUnitId, string source, dotcodedbEntities dotCodeDb)
        {
            if (!ValidationModel.CanUserCloneProject(codeUnitId)) return new CodeUnit();

            var autoid = new CodeUnitAutoId();
            dotCodeDb.CodeUnitAutoIds.Add(autoid);
            dotCodeDb.SaveChanges();

            var codeUnit = dotCodeDb.CodeUnits
                .Include("CodeUnitDllReferences")
                .SingleOrDefault(c => c.Id == codeUnitId);

            if (codeUnit == null) return null;

            var newCodeUnit = new CodeUnit
            {
                Id = Guid.NewGuid(),
                CreatedOn = DateTime.UtcNow,
                LanguageId = codeUnit.LanguageId,
                ModifiedOn = DateTime.UtcNow,
                Source = source,
                Title = codeUnit.Title,
                IsPublic = codeUnit.IsPublic,
                Description = codeUnit.Description,
                ParentId = codeUnit.Id,
                AutoId = autoid.Id
            };

            SetCodeUnitUserId(newCodeUnit);

            var newDllReferences = codeUnit.CodeUnitDllReferences
                .Select(reference => new CodeUnitDllReference()
                {
                    Id = Guid.NewGuid(),
                    CodeUnitId = newCodeUnit.Id,
                    BinaryId = reference.BinaryId,
                    CreatedOn = DateTime.UtcNow
                });

            dotCodeDb.CodeUnits.Add(newCodeUnit);
            newDllReferences.ToList().ForEach((d) => dotCodeDb.CodeUnitDllReferences.Add(d));

            dotCodeDb.SaveChanges();

            var compilerOutput = BuildModel.CompileInternal(source,
                                                ReferenceModel.GetProjectReferences(newCodeUnit.Id),
                                                Language.GetLanguageById(newCodeUnit.LanguageId));

            if (!compilerOutput.HasErrors)
            {
                BuildModel.SaveCompilerOutputToDb(new dotcodedbEntities(), newCodeUnit.Id, compilerOutput);   
            }

            return dotCodeDb.CodeUnits.Single(c => c.Id == newCodeUnit.Id);
        }

        private static void SetCodeUnitUserId(CodeUnit codeUnit)
        {
            // By default, all code is public.  Everything.
            codeUnit.IsPublic = true; //!WebSecurity.IsAuthenticated;
            codeUnit.UserId = !WebSecurity.IsAuthenticated ? (int?)null : WebSecurity.CurrentUserId;
        }

        public static CodeUnit GetDefaultProjectByLanguage(dotcode.lib.common.Language languageid)
        {
            var dotCodeDb = new dotcodedbEntities();
            var defaultCode = dotCodeDb.AdminSettings.Select(a => a.DefaultCSharpProjectId).FirstOrDefault() ?? Guid.Empty;
            if (defaultCode == Guid.Empty) return new CodeUnit();
            
            return GetCodeUnitById(defaultCode) ?? new CodeUnit();
        }
    }
}