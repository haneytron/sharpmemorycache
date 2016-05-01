using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Threading;

namespace SharpMemoryCache
{
    /// <summary>
    /// Operates identically to a regular MemoryCache, except that it properly initiates trims at the configured polling interval based on your memory settings.
    /// Usage: MemoryCache cache = new TrimmingMemoryCache(...);
    /// </summary>
    public class TrimmingMemoryCache : MemoryCache
    {
        // The function that sets the memory cache's last trim gen 2 count to a specific value
        private readonly Action<int> _setMemoryCacheLastTrimGen2CountFunc = null;
        // The timer that calls the last trim gen 2 count function
        private readonly Timer _setMemoryCacheLastTrimGen2CountTimer = null;
        // The integer incremented as the gen 2 count value
        private int _lastTrimGen2Count = 0;
        // Whether or not the polling interval has been set
        private bool _isPollingIntervalSet = false;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="name">The name to use to look up configuration information. Note: It is not required for configuration information to exist for every name. 
        /// If a matching configuration entry exists, the configuration information is used to configure the MemoryCache instance. If a matching configuration entry 
        /// does not exist, the name can be accessed through the Name property, because the specified name is associated with the MemoryCache instance. For information 
        /// about memory cache configuration, see MemoryCacheElement.</param>
        /// <param name="config">A collection of name/value pairs of configuration information to use for configuring the cache.</param>
        public TrimmingMemoryCache(string name, NameValueCollection config = null)
            : base(name, config)
        {
            // Use lambda expressions to create a set method for MemoryCache._stats._lastTrimGen2Count to circumvent poor functionality of MemoryCache
            // The default MemoryCache does not check for memory pressure except after a Gen 2 Garbage Collection. We want to do this more often than that.
            // So this method allows us to reset the field the MemoryCacheStatistics object uses periodically to a new value, to force the trim to be checked.

            // Define the types
            var memoryCacheType = typeof(MemoryCache);
            var memoryCacheStatisticsType = memoryCacheType.Assembly.GetType("System.Runtime.Caching.MemoryCacheStatistics", true);

            // Define the _stats field on MemoryCache
            var statsField = memoryCacheType.GetField("_stats", BindingFlags.Instance | BindingFlags.NonPublic);
            // Define the _lastTrimGen2Count field on MemoryCacheStatistics
            var lastTrimGen2CountField = memoryCacheStatisticsType.GetField("_lastTrimGen2Count", BindingFlags.Instance | BindingFlags.NonPublic);

            // Get a reference to this memory cache instance
            var targetExpression = Expression.Constant(this, typeof(MemoryCache));

            // Define the parameters to the method
            var valueExpression = Expression.Parameter(typeof(int), "value");

            // Create the field expressions
            var statsFieldExpression = Expression.Field(targetExpression, statsField);
            var lastTrimGen2CountFieldExpression = Expression.Field(statsFieldExpression, lastTrimGen2CountField);

            // Create the field value assignment expression
            var fieldValueAssignmentExpression = Expression.Assign(lastTrimGen2CountFieldExpression, valueExpression);

            // Compile to function
            _setMemoryCacheLastTrimGen2CountFunc = Expression.Lambda<Action<int>>(fieldValueAssignmentExpression, valueExpression).Compile();

            // Fire this method initially after a 1000 ms delay
            _setMemoryCacheLastTrimGen2CountTimer = new Timer(SetMemoryCacheLastTrimGen2Count, null, 1000, Timeout.Infinite);
        }

        /// <summary>
        /// Sets the MemoryCache._stats._lastTrimGen2Count field to a value.
        /// </summary>
        /// <param name="state">The state. Ignored.</param>
        private void SetMemoryCacheLastTrimGen2Count(object state)
        {
            // Set the value to force the trim to be executed instead of waiting for then next Gen 2 Garbage Collection
            _lastTrimGen2Count = _lastTrimGen2Count++ % 10;
            _setMemoryCacheLastTrimGen2CountFunc(_lastTrimGen2Count);

            // Check if we need to configure the timer's interval
            if (!_isPollingIntervalSet)
            {
                // Get the polling interval in seconds and divide it in half
                var halfPollingIntervalMilliseconds = (int)PollingInterval.TotalMilliseconds / 2;

                // Configure the timer to fire at half of the polling interval - this ensures the value is different when the MemoryCache code looks at it via polling
                _setMemoryCacheLastTrimGen2CountTimer.Change(halfPollingIntervalMilliseconds, halfPollingIntervalMilliseconds);

                // Mark set
                _isPollingIntervalSet = true;
            }
        }
    }
}
