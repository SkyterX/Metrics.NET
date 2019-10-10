using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

using Metrics.ConcurrencyUtilities;
using Metrics.MetricData;

namespace Metrics.Core
{
    public interface CounterImplementation : Counter, MetricValueProvider<CounterValue>
    {
    }

    public sealed class CounterMetric : CounterImplementation
    {
        public CounterValue Value
        {
            get
            {
                if (setCounters == null || setCounters.Count == 0)
                {
                    return new CounterValue(counter.GetValue());
                }
                return GetValueWithSetItems();
            }
        }

        public CounterValue GetValue(bool resetMetric = false)
        {
            var value = Value;
            if (resetMetric)
            {
                Reset();
            }
            return value;
        }

        public void Increment()
        {
            counter.Increment();
        }

        public void Increment(long value)
        {
            counter.Add(value);
        }

        public void Decrement()
        {
            counter.Decrement();
        }

        public void Decrement(long value)
        {
            counter.Add(-value);
        }

        public void Reset()
        {
            counter.Reset();
            if (setCounters != null)
            {
                foreach (var item in setCounters)
                {
                    item.Value.Reset();
                }
            }
        }

        public void Increment(string item)
        {
            Increment();
            SetCounter(item).Increment();
        }

        public void Increment(string item, long amount)
        {
            Increment(amount);
            SetCounter(item).Add(amount);
        }

        public void Decrement(string item)
        {
            Decrement();
            SetCounter(item).Decrement();
        }

        public void Decrement(string item, long amount)
        {
            Decrement(amount);
            SetCounter(item).Add(-amount);
        }

        private StripedLongAdder SetCounter(string item)
        {
            if (setCounters == null)
            {
                Interlocked.CompareExchange(ref setCounters, new ConcurrentDictionary<string, StripedLongAdder>(), null);
            }
            Debug.Assert(setCounters != null);
            return setCounters.GetOrAdd(item, v => new StripedLongAdder());
        }

        private CounterValue GetValueWithSetItems()
        {
            Debug.Assert(setCounters != null);
            var total = counter.GetValue();

            var items = new CounterValue.SetItem[setCounters.Count];
            var index = 0;
            foreach (var item in setCounters)
            {
                var itemValue = item.Value.GetValue();

                var percent = total > 0 ? itemValue / (double)total * 100 : 0.0;
                var setCounter = new CounterValue.SetItem(item.Key, itemValue, percent);
                items[index++] = setCounter;
                if (index == items.Length)
                {
                    break;
                }
            }

            Array.Sort(items, CounterValue.SetItemComparer);

            return new CounterValue(total, items);
        }

        private ConcurrentDictionary<string, StripedLongAdder> setCounters;

        private readonly StripedLongAdder counter = new StripedLongAdder();
    }
}