using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoTextFileExternalSorter<T> : ChoExternalSorter<T>
    {
        public ChoTextFileExternalSorter(IComparer<T> comparer, int capacity, int mergeCount)
            : base(comparer, capacity, mergeCount)
        {
        }

        protected override string Write(IEnumerable<T> run)
        {
#if _ALL_NET_
            var file = ChoPath.GetTempFileName();
            using (var writer = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new BinaryFormatter().Serialize(writer, run.ToArray());
            }
            return file;
#else
            throw new NotSupportedException("");
#endif
        }

        protected override IEnumerable<T> Read(string name)
        {
#if _ALL_NET_
            T[] arr = null;
            using (var reader = new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                arr = (T[])new BinaryFormatter().Deserialize(reader);
            }
            File.Delete(name);
            foreach (T t in arr)
                yield return t;
#else
            throw new NotSupportedException("");
#endif
        }
    }
}
