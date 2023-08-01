using AventusSharp.Data;
using AventusSharp.Routes.Attributes;
using AventusSharp.Routes.Response;
using System;
using System.Collections.Generic;


namespace AventusSharp.Routes
{
    public abstract class StorableRoute<T> : IRouter where T : IStorable
    {
        public virtual string StorableName()
        {
            Type t = typeof(T);
            string name = t.Name;
            if(t.IsInterface)
            {
                if (name.StartsWith("I"))
                {
                    return name.Substring(1);
                }
            }
            return name;
        }


        [Get, Route("/[StorableName]")]
        public ResultWithError<List<T>> GetAll()
        {
            return Storable<T>.GetAllWithError();
        }

        [Post, Route("/[StorableName]")]
        public ResultWithError<T> Create(T item)
        {
            return Storable<T>.CreateWithError(item);
        }

        [Get, Route("/[StorableName]/{id}")]
        public ResultWithError<T> GetById(int id)
        {
            return Storable<T>.GetByIdWithError(id);
        }

        [Put]
        [Route("/[StorableName]/{id}")]
        public ResultWithError<T> Update(int id, T body)
        {
            body.id = id;
            return Storable<T>.UpdateWithError(body);
        }

        [Delete, Route("/[StorableName]/{id}")]
        public ResultWithError<T> Delete(int id)
        {
            return Storable<T>.DeleteWithError(id);
        }
    }
}
