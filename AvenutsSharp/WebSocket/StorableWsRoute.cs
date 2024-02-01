using AventusSharp.Data;
using AventusSharp.Tools;
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
        public virtual ResultWithError<List<T>> GetAll()
        {
            ResultWithError<List<T>> result = DM_GetAll();
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
        protected virtual ResultWithError<List<T>> DM_GetAll()
        {
            return Storable<T>.GetAllWithError().ToGeneric();
        }

        [Path("/[StorableName]"), Broadcast]
        public virtual ResultWithError<T> Create(T item)
        {
            item = OnReceive(item);
            ResultWithError<T> result = DM_Create(item);
            if (result.Result != null)
            {
                result.Result = OnSend(item);
            }
            return result;
        }
        protected virtual ResultWithError<T> DM_Create(T item)
        {
            return Storable<T>.CreateWithError(item).ToGeneric();
        }

        [Path("/[StorableName]s"), Broadcast]
        public virtual ResultWithError<List<T>> CreateMany(List<T> list)
        {
            List<T> _list = new();
            foreach (T item in list)
            {
                _list.Add(OnReceive(item));
            }
            ResultWithError<List<T>> result = DM_CreateMany(_list);
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
        protected virtual ResultWithError<List<T>> DM_CreateMany(List<T> list)
        {
            return Storable<T>.CreateWithError(list).ToGeneric();
        }

        [Path("/[StorableName]/{id}")]
        public virtual ResultWithError<T> GetById(int id)
        {
            ResultWithError<T> result = DM_GetById(id);
            if (result.Result != null)
            {
                result.Result = OnSend(result.Result);
            }
            return result;
        }
        protected virtual ResultWithError<T> DM_GetById(int id)
        {
            return Storable<T>.GetByIdWithError(id).ToGeneric();
        }

        [Path("/[StorableName]/{id}"), Broadcast]
        public virtual ResultWithError<T> Update(int id, T item)
        {
            item.Id = id;
            item = OnReceive(item);
            ResultWithError<T> result = DM_Update(item);
            if (result.Result != null)
            {
                result.Result = OnSend(item);
            }
            return result;
        }
        protected virtual ResultWithError<T> DM_Update(T item)
        {
            return Storable<T>.UpdateWithError(item).ToGeneric();
        }

        [Path("/[StorableName]s"), Broadcast]
        public virtual ResultWithError<List<T>> UpdateMany(List<T> list)
        {
            List<T> _list = new();
            foreach (T item in list)
            {
                _list.Add(OnReceive(item));
            }
            ResultWithError<List<T>> result = DM_UpdateMany(_list);
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

        protected virtual ResultWithError<List<T>> DM_UpdateMany(List<T> list)
        {
            return Storable<T>.UpdateWithError(list).ToGeneric();
        }

        [Path("/[StorableName]/{id}"), Broadcast]
        public virtual ResultWithError<T> Delete(int id)
        {
            ResultWithError<T> result = DM_Delete(id);
            if (result.Result != null)
            {
                result.Result = OnSend(result.Result);
            }
            return result;
        }
        protected virtual ResultWithError<T> DM_Delete(int id)
        {
            return Storable<T>.DeleteWithError(id).ToGeneric();
        }

        [Path("/[StorableName]s"), Broadcast]
        public virtual ResultWithError<List<T>> DeleteMany(List<int> ids)
        {
            ResultWithError<List<T>> result = DM_DeleteMany(ids);
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

        protected virtual ResultWithError<List<T>> DM_DeleteMany(List<int> ids)
        {
            return Storable<T>.DeleteWithError(ids).ToGeneric();
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
