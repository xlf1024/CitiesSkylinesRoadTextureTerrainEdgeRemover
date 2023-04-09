using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoadTextureTerrainEdgeRemover
{
    struct Tuple<T, U> : IEquatable<Tuple<T, U>>
    {
        readonly T first;
        readonly U second;

        public Tuple(T first, U second)
        {
            this.first = first;
            this.second = second;
        }

        public T First { get { return first; } }
        public U Second { get { return second; } }

        public override int GetHashCode()
        {
            return first.GetHashCode() ^ second.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals((Tuple<T, U>)obj);
        }

        public bool Equals(Tuple<T, U> other)
        {
            return other.first.Equals(first) && other.second.Equals(second);
        }

        public override string ToString()
        {
            return "Tuple[ " + first.ToString() + "," + second.ToString() + "]";
        }
    }

}
