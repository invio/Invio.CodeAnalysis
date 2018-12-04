using System;
using System.Linq;

namespace Invio.CodeAnalysis.Example {
    public static class Helper {
        public static IQueryable<Data> WhereValueIsFoo(IQueryable<Data> source) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Where(d => d.Value == "Foo");
        }
    }

    public class Data {
        public String Value { get; set; }
    }
}