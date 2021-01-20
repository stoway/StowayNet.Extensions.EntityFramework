using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StowayNet
{
    public enum ConditionOperator
    {
        /// <summary>
        /// 等于
        /// </summary>
        Equal = 1,
        /// <summary>
        /// 大于
        /// </summary>
        GreaterThan = 2,
        /// <summary>
        /// 小于
        /// </summary>
        LessThan = 3,
        /// <summary>
        /// 大于等于
        /// </summary>
        GreaterThanOrEqual = 4,
        /// <summary>
        /// 小于等于
        /// </summary>
        LessThanOrEqual = 5,
        /// <summary>
        /// 不等于
        /// </summary>
        NotEqual = 6,
        /// <summary>
        /// 并且
        /// </summary>
        //And,
        /// <summary>
        /// 或者
        /// </summary>
        //Or,
        /// <summary>
        /// 包含
        /// </summary>
        Contains = 7,
        /// <summary>
        /// Value1 与 Value2 之间
        /// </summary>
        Between = 8,
        /// <summary>
        /// 
        /// </summary>
        In = 9
    }

}
