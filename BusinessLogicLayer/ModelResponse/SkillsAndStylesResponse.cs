using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class SkillsAndStylesResponse
    {
        public List<SkillResponse> Skills { get; set; }
        public List<DecorationStyleResponse> DecorationStyles { get; set; }
    }

    public class SkillResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class DecorationStyleResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
