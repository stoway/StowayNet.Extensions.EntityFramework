using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StowayNet
{
    public class ConditionEntity
    {
        public string FieldName { get; set; }

        public object Value { get; set; }

        public object Value2 { get; set; }

        public ConditionOperator Operator { get; set; } = ConditionOperator.Equal;
    }
}
