using System.Runtime.Serialization;

namespace dotcode.lib.common
{
    [DataContract]
    public class Language
    {
        public static Language CSharp5 = new Language(1, "C#", "5");
        public static Language FSharp3 = new Language(2, "F#", "3");
        
        [DataMember]
        public int Id { get; set; }
        
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Version { get; set; }

        private Language(int id, string name, string version)
        {
            this.Id = id;
            this.Name = name;
            this.Version = version;
        }

        public static Language GetLanguageById(int? language)
        {
            switch (language)
            {
                case 1:
                    return CSharp5;
                case 2:
                    return FSharp3;
                default:
                    return CSharp5;
            }
        }
    }
}
