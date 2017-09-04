using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CapeCode.ExtendedList
{
    public class ExtendedList<T> : IList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable {

        public bool ThreadSafe { get; set; }

        #region ItemChanged
        public delegate void DELItemChanged( ChangeArgs<T> args );
        public event DELItemChanged ItemChanged;

        private void OnItemChanged( ChangeType changeType, T item ) {
            if ( ItemChanged != null )
                ItemChanged( new ChangeArgs<T>() { ChangeType = changeType, Item = item } );
        }
        #endregion

        private List<T> list;

        public ExtendedList() {
            list = new List<T>();
        }

        public ExtendedList( bool threadsafe )
            : this() {
            this.ThreadSafe = threadsafe;
        }

        public ExtendedList( IEnumerable<T> collection ) {
            list = new List<T>( collection );
        }

        public ExtendedList( int capacity ) {
            list = new List<T>( capacity );
        }

        public ExtendedList( IEnumerable<T> collection, IList<T> mirror )
            : this( collection ) {
            this.ItemChanged += delegate( ChangeArgs<T> args ) {
                switch ( args.ChangeType ) {
                    case ChangeType.Add:
                        mirror.Add( args.Item );
                        break;
                    case ChangeType.Remove:
                        mirror.Remove( args.Item );
                        break;
                }
            };
        }

        public int Capacity { get { return list.Capacity; } set { list.Capacity = value; } }

        public int Count { get { return list.Count; } }

        public T this[ int index ] {
            get {
                if ( ThreadSafe ) {
                    lock ( ( this as ICollection ).SyncRoot ) {
                        return list[ index ];
                    }
                } else {
                    return list[ index ];
                }
            }
            set {
                if ( ThreadSafe ) {
                    lock ( ( this as ICollection ).SyncRoot ) {
                        SetItemOnIndex( index, value );
                    }
                } else {
                    SetItemOnIndex( index, value );
                }
            }
        }

        private void SetItemOnIndex( int index, T value ) {
            if ( list[ index ].GetHashCode() != value.GetHashCode() ) {
                OnItemChanged( ChangeType.Remove, list[ index ] );
                list[ index ] = value;
                OnItemChanged( ChangeType.Add, value );
            }
        }

        public void Add( T item ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.Add( item );
                    OnItemChanged( ChangeType.Add, item );
                }
            } else {
                list.Add( item );
                OnItemChanged( ChangeType.Add, item );
            }
        }

        public void AddRange( IEnumerable<T> collection ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.AddRange( collection );
                    foreach ( T item in collection )
                        OnItemChanged( ChangeType.Add, item );
                }
            } else {
                list.AddRange( collection );
                foreach ( T item in collection )
                    OnItemChanged( ChangeType.Add, item );
            }
        }

        public ReadOnlyCollection<T> AsReadOnly() {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.AsReadOnly();
                }
            } else {
                return list.AsReadOnly();
            }
        }

        public int BinarySearch( T item ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.BinarySearch( item );
                }
            } else {
                return list.BinarySearch( item );
            }

        }

        public int BinarySearch( T item, IComparer<T> comparer ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.BinarySearch( item, comparer );
                }
            } else {
                return list.BinarySearch( item, comparer );
            }
        }

        public int BinarySearch( int index, int count, T item, IComparer<T> comparer ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.BinarySearch( index, count, item, comparer );
                }
            } else {
                return list.BinarySearch( index, count, item, comparer );
            }
        }

        public void Clear() {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.ForEach( item => OnItemChanged( ChangeType.Remove, item ) );
                    list.Clear();
                }
            } else {
                list.ForEach( item => OnItemChanged( ChangeType.Remove, item ) );
                list.Clear();
            }
        }

        public bool Contains( T item ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.Contains( item );
                }
            } else {
                return list.Contains( item );
            }
        }

        public List<TOutput> ConvertAll<TOutput>( Converter<T, TOutput> converter ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.ConvertAll<TOutput>( converter );
                }
            } else {
                return list.ConvertAll<TOutput>( converter );
            }
        }

        public void CopyTo( T[] array ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.CopyTo( array );
                }
            } else {
                list.CopyTo( array );
            }
        }

        public void CopyTo( T[] array, int arrayIndex ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.CopyTo( array, arrayIndex );
                }
            } else {
                list.CopyTo( array, arrayIndex );
            }
        }

        public void CopyTo( int index, T[] array, int arrayIndex, int count ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.CopyTo( index, array, arrayIndex, count );
                }
            } else {
                list.CopyTo( index, array, arrayIndex, count );
            }
        }

        public bool Exists( Predicate<T> match ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.Exists( match );
                }
            } else {
                return list.Exists( match );
            }
        }

        public T Find( Predicate<T> match ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.Find( match );
                }
            } else {
                return list.Find( match );
            }
        }

        public List<T> FindAll( Predicate<T> match ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.FindAll( match );
                }
            } else {
                return list.FindAll( match );
            }
        }

        public int FindIndex( Predicate<T> match ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.FindIndex( match );
                }
            } else {
                return list.FindIndex( match );
            }
        }

        public int FindIndex( int startIndex, Predicate<T> match ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.FindIndex( startIndex, match );
                }
            } else {
                return list.FindIndex( startIndex, match );
            }
        }

        public int FindIndex( int startIndex, int count, Predicate<T> match ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.FindIndex( startIndex, count, match );
                }
            } else {
                return list.FindIndex( startIndex, count, match );
            }
        }

        public T FindLast( Predicate<T> match ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.FindLast( match );
                }
            } else {
                return list.FindLast( match );
            }
        }

        public int FindLastIndex( Predicate<T> match ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.FindLastIndex( match );
                }
            } else {
                return list.FindLastIndex( match );
            }
        }

        public int FindLastIndex( int startIndex, Predicate<T> match ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.FindLastIndex( startIndex, match );
                }
            } else {
                return list.FindLastIndex( startIndex, match );
            }
        }

        public int FindLastIndex( int startIndex, int count, Predicate<T> match ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.FindLastIndex( startIndex, count, match );
                }
            } else {
                return list.FindLastIndex( startIndex, count, match );
            }
        }

        public void ForEach( Action<T> action ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.ForEach( action );
                }
            } else {
                list.ForEach( action );
            }
        }

        public List<T>.Enumerator GetEnumerator() {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.GetEnumerator();
                }
            } else {
                return list.GetEnumerator();
            }
        }

        public List<T> GetRange( int index, int count ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.GetRange( index, count );
                }
            } else {
                return list.GetRange( index, count );
            }
        }

        public int IndexOf( T item ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.IndexOf( item );
                }
            } else {
                return list.IndexOf( item );
            }
        }

        public int IndexOf( T item, int index ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.IndexOf( item, index );
                }
            } else {
                return list.IndexOf( item, index );
            }
        }

        public int IndexOf( T item, int index, int count ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.IndexOf( item, index, count );
                }
            } else {
                return list.IndexOf( item, index, count );
            }
        }

        public void Insert( int index, T item ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.Insert( index, item );
                    OnItemChanged( ChangeType.Add, item );
                }
            } else {
                list.Insert( index, item );
                OnItemChanged( ChangeType.Add, item );
            }
        }

        public void InsertRange( int index, IEnumerable<T> collection ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.InsertRange( index, collection );
                    foreach ( T item in collection )
                        OnItemChanged( ChangeType.Add, item );
                }
            } else {
                list.InsertRange( index, collection );
                foreach ( T item in collection )
                    OnItemChanged( ChangeType.Add, item );
            }
        }

        public int LastIndexOf( T item ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.LastIndexOf( item );
                }
            } else {
                return list.LastIndexOf( item );
            }
        }

        public int LastIndexOf( T item, int index ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.LastIndexOf( item, index );
                }
            } else {
                return list.LastIndexOf( item, index );
            }
        }

        public int LastIndexOf( T item, int index, int count ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.LastIndexOf( item, index, count );
                }
            } else {
                return list.LastIndexOf( item, index, count );
            }
        }

        public bool Remove( T item ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    if ( list.Remove( item ) ) {
                        OnItemChanged( ChangeType.Remove, item );
                        return true;
                    } else {
                        return false;
                    }
                }
            } else {
                if ( list.Remove( item ) ) {
                    OnItemChanged( ChangeType.Remove, item );
                    return true;
                } else {
                    return false;
                }
            }
        }

        public int RemoveAll( Predicate<T> match ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    var test = list.FindAll( match );
                    foreach ( var item in list.FindAll( match ) ) {
                        OnItemChanged( ChangeType.Remove, item );
                        list.Remove( item );
                    }
                    return 0;
                }
            } else {
                foreach ( var item in list.FindAll( match ) ) {
                    OnItemChanged( ChangeType.Remove, item );
                    list.Remove( item );
                }
                return 0;
            }
        }

        public void RemoveAt( int index ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    OnItemChanged( ChangeType.Remove, this[ index ] );
                    list.RemoveAt( index );
                }
            } else {
                OnItemChanged( ChangeType.Remove, this[ index ] );
                list.RemoveAt( index );
            }
        }

        public void RemoveRange( int index, int count ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.GetRange( index, count ).ForEach( item => OnItemChanged( ChangeType.Remove, item ) );
                    list.RemoveRange( index, count );
                }
            } else {
                list.GetRange( index, count ).ForEach( item => OnItemChanged( ChangeType.Remove, item ) );
                list.RemoveRange( index, count );
            }
        }

        public void Reverse() {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.Reverse();
                }
            } else {
                list.Reverse();
            }
        }

        public void Reverse( int index, int count ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.Reverse( index, count );
                }
            } else {
                list.Reverse( index, count );
            }
        }

        public void Sort() {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.Sort();
                }
            } else {
                list.Sort();
            }
        }

        public void Sort( Comparison<T> comparison ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.Sort( comparison );
                }
            } else {
                list.Sort( comparison );
            }
        }

        public void Sort( IComparer<T> comparer ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.Sort( comparer );
                }
            } else {
                list.Sort( comparer );
            }
        }

        public void Sort( int index, int count, IComparer<T> comparer ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.Sort( index, count, comparer );
                }
            } else {
                list.Sort( index, count, comparer );
            }
        }

        public T[] ToArray() {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.ToArray();
                }
            } else {
                return list.ToArray();
            }
        }

        public void TrimExcess() {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    list.TrimExcess();
                }
            } else {
                list.TrimExcess();
            }
        }

        public bool TrueForAll( Predicate<T> match ) {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return list.TrueForAll( match );
                }
            } else {
                return list.TrueForAll( match );
            }
        }

        #region ICollection<T> Members

        public bool IsReadOnly {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return this.GetEnumerator();
                }
            } else {
                return this.GetEnumerator();
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() {
            if ( ThreadSafe ) {
                lock ( ( this as ICollection ).SyncRoot ) {
                    return this.GetEnumerator();
                }
            } else {
                return this.GetEnumerator();
            }
        }

        #endregion

        #region IList Member

        public int Add( object value ) {
            Add( ( T ) value );
            return 1;
        }

        public bool Contains( object value ) {
            return Contains( ( T ) value );
        }

        public int IndexOf( object value ) {
            return IndexOf( ( T ) value );
        }

        public void Insert( int index, object value ) {
            Insert( index, ( T ) value );
        }

        public bool IsFixedSize {
            get { return ( list as IList ).IsFixedSize; }
        }

        public void Remove( object value ) {
            Remove( ( T ) value );
        }

        object IList.this[ int index ] {
            get { return this[ index ]; }
            set { this[ index ] = ( T ) value; }
        }

        #endregion

        #region ICollection Member

        public void CopyTo( Array array, int index ) {
            ( list as ICollection ).CopyTo( array, index );
        }

        public bool IsSynchronized {
            get { return ( list as ICollection ).IsSynchronized; }
        }

        public object SyncRoot {
            get { return ( list as ICollection ).SyncRoot; }
        }

        #endregion
    }
}
