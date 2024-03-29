﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace AventusSharp.Scheduler.Util
{
    internal class ScheduleCollection
    {
        private readonly List<Schedule> _schedules = new List<Schedule>();

        private readonly object _lock = new object();

        internal bool Any()
        {
            lock (_lock)
            {
                return _schedules.Any();
            }
        }

        internal void Sort()
        {
            lock (_lock)
            {
                _schedules.Sort((x, y) => DateTime.Compare(x.NextRun, y.NextRun));
            }
        }

        internal IEnumerable<Schedule> All()
        {
            lock (_lock)
            {
                return _schedules;
            }
        }

        internal void Add(Schedule schedule)
        {
            lock (_lock)
            {
                _schedules.Add(schedule);
            }
        }

        internal bool Has(string name)
        {
            lock (_lock)
            {
                Schedule? schedule = Get(name);
                if (schedule == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        internal bool Remove(string name)
        {
            lock (_lock)
            {
                Schedule? schedule = Get(name);
                if (schedule == null)
                {
                    return false;
                }
                else
                {
                    _schedules.Remove(schedule);
                    return true;
                }
            }
        }

        internal bool Remove(Schedule schedule)
        {
            lock (_lock)
            {
                return _schedules.Remove(schedule);
            }
        }

        internal void RemoveAll()
        {
            lock (_lock)
            {
                _schedules.Clear();
            }
        }

        internal Schedule? First()
        {
            lock (_lock)
            {
                return _schedules.FirstOrDefault();
            }
        }

        internal Schedule? Get(string name)
        {
            lock (_lock)
            {
                return _schedules.FirstOrDefault(x => x.Name == name);
            }
        }
    }
}
