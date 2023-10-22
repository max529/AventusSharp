using AventusSharp.Data;
using AventusSharp.WebSocket.Attributes;
using System;
using System.Collections.Generic;

namespace AventusSharp.WebSocket
{
    public abstract class StorableWsRoute<T> : WsRoute where T : IStorable
    {

        protected virtual string StorableName()
        {
            Type t = typeof(T);
            string name = t.Name;
            if (t.IsInterface)
            {
                if (name.StartsWith("I"))
                {
                    return name.Substring(1);
                }
            }
            return name;
        }


        [Path("/[StorableName]")]
        public virtual ResultWithDataError<List<T>> GetAll()
        {
            ResultWithDataError<List<T>> result = DM_GetAll();
            if (result.Result != null)
            {
                List<T> list = new();
                foreach (T item in result.Result)
                {
                    list.Add(OnSend(item));
                }
                result.Result = list;
            }
            return result;
        }
        protected virtual ResultWithDataError<List<T>> DM_GetAll()
        {
            return Storable<T>.GetAllWithError();
        }

        [Path("/[StorableName]"), Broadcast]
        public virtual ResultWithDataError<T> Create(T item)
        {
            item = OnReceive(item);
            ResultWithDataError<T> result = DM_Create(item);
            if (result.Result != null)
            {
                result.Result = OnSend(item);
            }
            return result;
        }
        protected virtual ResultWithDataError<T> DM_Create(T item)
        {
            return Storable<T>.CreateWithError(item);
        }

        [Path("/[StorableName]s"), Broadcast]
        public virtual ResultWithDataError<List<T>> CreateMany(List<T> list)
        {
            List<T> _list = new();
            foreach (T item in list)
            {
                _list.Add(OnReceive(item));
            }
            ResultWithDataError<List<T>> result = DM_CreateMany(_list);
            if (result.Result != null)
            {
                List<T> listTemp = new();
                foreach (T item in result.Result)
                {
                    listTemp.Add(OnSend(item));
                }
                result.Result = listTemp;
            }

            return result;
        }
        protected virtual ResultWithDataError<List<T>> DM_CreateMany(List<T> list)
        {
            return Storable<T>.CreateWithError(list);
        }

        [Path("/[StorableName]/{id}")]
        public virtual ResultWithDataError<T> GetById(int id)
        {
            ResultWithDataError<T> result = DM_GetById(id);
            if (result.Result != null)
            {
                result.Result = OnSend(result.Result);
            }
            return result;
        }
        protected virtual ResultWithDataError<T> DM_GetById(int id)
        {
            return Storable<T>.GetByIdWithError(id);
        }

        [Path("/[StorableName]/{id}"), Broadcast]
        public virtual ResultWithDataError<T> Update(int id, T item)
        {
            item.Id = id;
            item = OnReceive(item);
            ResultWithDataError<T> result = DM_Update(item);
            if (result.Result != null)
            {
                result.Result = OnSend(item);
            }
            return result;
        }
        protected virtual ResultWithDataError<T> DM_Update(T item)
        {
            return Storable<T>.UpdateWithError(item);
        }

        [Path("/[StorableName]s"), Broadcast]
        public virtual ResultWithDataError<List<T>> UpdateMany(List<T> list)
        {
            List<T> _list = new();
            foreach (T item in list)
            {
                _list.Add(OnReceive(item));
            }
            ResultWithDataError<List<T>> result = DM_UpdateMany(_list);
            if (result.Result != null)
            {
                List<T> listTemp = new();
                foreach (T item in result.Result)
                {
                    listTemp.Add(OnSend(item));
                }
                result.Result = listTemp;
            }

            return result;
        }

        protected virtual ResultWithDataError<List<T>> DM_UpdateMany(List<T> list)
        {
            return Storable<T>.UpdateWithError(list);
        }

        [Path("/[StorableName]/{id}"), Broadcast]
        public virtual ResultWithDataError<T> Delete(int id)
        {
            ResultWithDataError<T> result = DM_Delete(id);
            if (result.Result != null)
            {
                result.Result = OnSend(result.Result);
            }
            return result;
        }
        protected virtual ResultWithDataError<T> DM_Delete(int id)
        {
            return Storable<T>.DeleteWithError(id);
        }

        [Path("/[StorableName]s"), Broadcast]
        public virtual ResultWithDataError<List<T>> DeleteMany(List<int> ids)
        {
            ResultWithDataError<List<T>> result = DM_DeleteMany(ids);
            if (result.Result != null)
            {
                List<T> listTemp = new();
                foreach (T item in result.Result)
                {
                    listTemp.Add(OnSend(item));
                }
                result.Result = listTemp;
            }

            return result;
        }

        protected virtual ResultWithDataError<List<T>> DM_DeleteMany(List<int> ids)
        {
            return Storable<T>.DeleteWithError(ids);
        }


        protected virtual T OnReceive(T item)
        {
            return item;
        }
        protected virtual T OnSend(T item)
        {
            return item;
        }
    }
}
