using AventusSharp.Data;
using AventusSharp.Route.Attributes;
using AventusSharp.Route.Response;
using System.Collections.Generic;
using RouteAttr = AventusSharp.Route.Attributes.Route;

namespace AventusSharp.Route
{
    public abstract class StorableRoute<T> : IRouter where T : IStorable
    {
        public virtual string GetName()
        {
            return typeof(T).Name;
        }


        [Get, RouteAttr("/[GetName]")]
        public ResultWithError<List<T>> GetAll()
        {
            return Storable<T>.GetAllWithError();
        }

        [Post, RouteAttr("/[GetName]")]
        public T? Create(T item)
        {
            return Storable<T>.Create(item);
        }

        [Get, RouteAttr("/[GetName]/{id}")]
        public T GetById(int id)
        {
            return Storable<T>.GetById(id);
        }

        [Put]
        [RouteAttr("/[GetName]/{id}")]
        public T? Update(int id, T body)
        {
            body.id = id;
            T? result = Storable<T>.Update(body);
            return result;
        }

        [Delete, RouteAttr("/[GetName]/{id}")]
        public T? Delete(int id)
        {
            T item = Storable<T>.GetById(id);
            if (item != null)
            {
                Storable<T>.Delete(item);
            }
            return item;
        }
    }
}
