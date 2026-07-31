using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schala
{
    public class ExclusiveList<T> : IList<T> where T : IComparable<T>
    {
        #region Data
        private List<T> _list;
        IComparer<T>? Comparer = null;
        private int _maxSize;
        public int MaxSize
        {
            get
            {
                return _maxSize;
            }
            set
            {
                _maxSize = value;
                //Make sure to trim the array if we reduce the size down.
                if (_list != null && _maxSize < _list.Count)
                    _list.RemoveRange(_maxSize, _list.Count - _maxSize);
            }
        }
        #endregion
        #region Constructors
        public ExclusiveList()
        {
            _list = new List<T>(MaxSize);
            if (Comparer == null)
                this.Comparer = HighestComparer;

        }

        public ExclusiveList(int MaxSize) : this()
        {
            if (this.MaxSize == 0)
                this.MaxSize = MaxSize;
        }

        public ExclusiveList(IComparer<T> Comparer) : this(10)
        {
            if (Comparer != null)
                this.Comparer = Comparer;
            else
                this.Comparer = HighestComparer;
        }

        public ExclusiveList(IComparer<T> Comparer, int MaxSize) : this(Comparer)
        {
            if (MaxSize < 1)
                throw new ArgumentOutOfRangeException("Max size cannot be smaller than 1.");
            this.MaxSize = MaxSize;
        }
        #endregion
        #region Default comparers for highest and lowest
        public static Comparer<T> HighestComparer = Comparer<T>.Create((x, y) => { return x.CompareTo(y) * -1; });
        public static Comparer<T> LowestComparer = Comparer<T>.Create((x, y) => { return x.CompareTo(y); });
        #endregion
        #region Implement IList<T>
        public T this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            if (Comparer == null)
                return;
            
            //Small speed hack to skip to big check if we're already out of range.
            if (_list.Count == MaxSize && Comparer.Compare(item, _list.Last()) > -1)
                return;

            int i = _list.BinarySearch(0, _list.Count, item, Comparer);
            if (i >= 0)
                Insert(i, item);
            else
                Insert(~i, item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
            if (_list.Count > MaxSize)
                _list.RemoveAt(_list.Count - 1);
        }

        public bool Remove(T item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        #endregion
    }
}
