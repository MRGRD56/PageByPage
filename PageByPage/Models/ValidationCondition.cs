using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PageByPage.Models
{
    public record ValidationCondition(
        ValidationPredicate Predicate,
        string ErrorText,
        ValidationCallback Callback = null);
}
