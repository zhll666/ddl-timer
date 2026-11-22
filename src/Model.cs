using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ChronoRuler
{
    [DataContract]
    public class Project
    {
        [DataMember(Name = "id")]
        public string Id;

        [DataMember(Name = "ordinal")]
        public int Ordinal;

        [DataMember(Name = "name")]
        public string Name;

        [DataMember(Name = "color")]
        public int Color;

        [DataMember(Name = "created")]
        public long Created;

        [DataMember(Name = "endAt")]
        public long EndAt;
    }

    [DataContract]
    public class AppData
    {
        [DataMember(Name = "projects")]
        public List<Project> Projects;

        [DataMember(Name = "zoom")]
        public int Zoom;

        [DataMember(Name = "pxHour")]
        public double PxHour;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Projects = new List<Project>();
        }

        public AppData()
        {
            Projects = new List<Project>();
            Zoom = 1;
            PxHour = 120;
        }

        public Project FindProject(string id)
        {
            if (string.IsNullOrEmpty(id) || Projects == null) return null;
            for (int i = 0; i < Projects.Count; i++)
            {
                if (Projects[i] != null && Projects[i].Id == id) return Projects[i];
            }
            return null;
        }

        public int ProjectIndex(string id)
        {
            if (string.IsNullOrEmpty(id) || Projects == null) return -1;
            for (int i = 0; i < Projects.Count; i++)
            {
                if (Projects[i] != null && Projects[i].Id == id) return i;
            }
            return -1;
        }
    }
}
