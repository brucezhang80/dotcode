using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using JitJotData;
using Mirrors;
using dotcode.lib.common;

namespace dotcode.Models
{
    public enum ReferenceType : int
    {
        Internal = 1,
        Common = 2,
        User = 3
    }

    public static class ReferenceModel
    {
        // We need to bootstrap the data in the database.
        //  But for now, we are hardcoding a few things and doing it manually.
        const int CommonLibraryId = 2;
        private const int UserLibraryId = 3;

        public static IEnumerable<Guid> GetProjectReferencedBinaryIds(Guid codeUnitId)
        {
            var dotCodeDb = new dotcodedbEntities();
            return dotCodeDb
                .CodeUnitDllReferences
                .Where(cur => !cur.IsTemporaryReference)
                .Where(cur => cur.CodeUnitId == codeUnitId)
                .Select(cur => cur.BinaryId );
        }

        public static IEnumerable<Binary> GetProjectCustomReferences(ClientModel clientModel)
        {
            var dotCodeDb = new dotcodedbEntities();
            return dotCodeDb.CodeUnitDllReferences
                .Include("Binary")
                .Where(cur => cur.CodeUnitId == clientModel.Id && cur.Binary.Type == (int)ReferenceType.User)
                .Where(cur => clientModel.References.Contains(cur.BinaryId))
                .Select(cur => new { cur.Binary.Description, cur.Binary.Id, cur.Binary.RuntimeVersion, cur.Binary.FileName})
                .ToArray()
                .Select(a => new Binary
                    {
                        Id = a.Id,
                        Description = a.Description,
                        FileName = a.FileName,
                        RuntimeVersion = a.RuntimeVersion
                    });

        }

        public static IEnumerable<Binary> GetUploadedReferences()
        {
            return GetReferencesByTypeId(UserLibraryId);
        }

        public static IEnumerable<Binary> GetCommonReferences()
        {
            return GetReferencesByTypeId(CommonLibraryId);
        }

        private static IEnumerable<Binary> GetReferencesByTypeId(int typeId)
        {
            var dotCodeDb = new dotcodedbEntities();
            var refs = dotCodeDb.Binaries.Where(d => d.Type == typeId);
            return refs;
        }

        public static IEnumerable<Guid> GetProjectReferences(Guid codeunitid)
        {
            var dotCodeDb = new dotcodedbEntities();
            var refs = dotCodeDb.CodeUnitDllReferences
                .Where(r => r.CodeUnitId == codeunitid && !r.IsTemporaryReference)
                .Select(r => r.BinaryId);
            return refs.ToList();
        }

        public static Guid[] UploadCustomProjectReference(ClientModel clientModel, IEnumerable<HttpPostedFileBase> files)
        {
            var dotcodeDb = new dotcodedbEntities();
            var dllRefIds = (clientModel.References ?? Enumerable.Empty<Guid>()).ToList();

            foreach (var file in files)
            {
                if (file == null || file.ContentLength == 0) continue;
                if (String.IsNullOrWhiteSpace(file.FileName)) continue;
                if (!file.FileName.ToLower().EndsWith(".dll")) continue;

                Mirror mirror;
                var rawBinary = StreamToByteArray(file.InputStream);

                try
                {
                    mirror = new Mirror(rawBinary);
                }
                catch
                {
                    continue;
                }

                var bin = new Binary
                {
                    Id = Guid.NewGuid(),
                    RawAssembly = rawBinary,
                    FileName = file.FileName,
                    Type = (int)ReferenceType.User,
                    RuntimeVersion = mirror.GetImageRuntimeVersion(),
                    Description = mirror.GetAssemblyFileVersionInfo(rawBinary).Comments,
                    CreatedOn = DateTime.UtcNow
                };

                var codeUnitRef = new CodeUnitDllReference
                {
                    Id = Guid.NewGuid(),
                    CodeUnitId = clientModel.Id,
                    BinaryId = bin.Id,
                    IsTemporaryReference = true,
                    CreatedOn = DateTime.UtcNow
                };

                dotcodeDb.Binaries.Add(bin);
                dotcodeDb.CodeUnitDllReferences.Add(codeUnitRef);

                dllRefIds.Add(bin.Id);
            }

            dotcodeDb.SaveChanges();

            return dllRefIds.ToArray();
        }

        public static void DeleteCustomProjectReference(Guid projectid, Guid binaryId)
        {
            var dotcodeDb = new dotcodedbEntities();
            var reference = dotcodeDb.CodeUnitDllReferences
                .Include("DllReference.Binary")
                .SingleOrDefault(cur => cur.CodeUnitId == projectid && cur.BinaryId == binaryId);

            if (reference == null) return;

            dotcodeDb.CodeUnitDllReferences.Remove(reference);
            dotcodeDb.SaveChanges();
            RemovedUnreferencedBinaries();
        }

        public static void SetProjectReferences(Guid dcProjectId, IEnumerable<Guid> refids)
        {
            if (!ValidationModel.CanUserSetProjectReferences(dcProjectId)) return;

            // If no references are selected, refids will be null.  Set to an empty array so
            // references will be cleared.

            if (refids == null) refids = Enumerable.Empty<Guid>();
            if (dcProjectId == Guid.Empty || refids.Count() > 64) return;

            var dotCodeDb = new dotcodedbEntities();
            var hasInvalidReferences = refids.Any(guid => guid == Guid.Empty || !dotCodeDb.Binaries.Any(d => d.Id == guid));
            if (hasInvalidReferences) return;

            var projectReferences = dotCodeDb.CodeUnitDllReferences
                .Where(cur => cur.CodeUnitId == dcProjectId)
                .Include("Binary")
                .ToList();

            // Clear all references for this project
            foreach (var reference in projectReferences)
            {
                dotCodeDb.CodeUnitDllReferences.Remove(reference);
            }

            // Add selected
            foreach (var guid in refids)
            {
                dotCodeDb.CodeUnitDllReferences.Add(new CodeUnitDllReference
                {
                    Id = Guid.NewGuid(),
                    CodeUnitId = dcProjectId,
                    BinaryId = guid,
                    CreatedOn = DateTime.UtcNow
                });
            }

            dotCodeDb.SaveChanges();

            // Now we need to go through DllReferences of User type
            // and remove ones that are not referenced.  No filter by project, just grab everything.
            RemovedUnreferencedBinaries();
        }

        private static byte[] StreamToByteArray(Stream stream)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        // We need to trim unreferenced binaries
        private static void RemovedUnreferencedBinaries()
        {
            var dotCodeDb = new dotcodedbEntities();

            // Grab all dll references where type = custom/user
            // where not in codeunitdllreferences
            // delete dllref, binary
            var customdllrefs =
                dotCodeDb.Binaries
                    .Where(d => d.Type == (int)ReferenceType.User)
                    .Include("CodeUnitDllReferences")
                    .Where(d => d.CodeUnitDllReferences.Count == 0)
                    .Select(dllRef => new { DllRefId = dllRef.Id, BinaryId = dllRef.Id });

            foreach (var customdllref in customdllrefs)
            {
                dotCodeDb.Database.ExecuteSqlCommand("DELETE FROM Binaries WHERE ID = {0}", customdllref.BinaryId);
            }
        }
    }
}
