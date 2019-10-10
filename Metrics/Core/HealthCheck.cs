using System;

namespace Metrics.Core
{
    public class HealthCheck
    {
        protected HealthCheck(string name)
            : this(name, () => { })
        {
        }

        public HealthCheck(string name, Action check)
            : this(name, () =>
                {
                    check();
                    return string.Empty;
                })
        {
        }

        public HealthCheck(string name, Func<string> check)
            : this(name, () => HealthCheckResult.Healthy(check()))
        {
        }

        public HealthCheck(string name, Func<HealthCheckResult> check)
        {
            Name = name;
            this.check = check;
        }

        public struct Result
        {
            public Result(string name, HealthCheckResult check)
            {
                Name = name;
                Check = check;
            }

            public readonly string Name;
            public readonly HealthCheckResult Check;
        }

        public string Name { get; }

        protected virtual HealthCheckResult Check()
        {
            return check();
        }

        public Result Execute()
        {
            try
            {
                return new Result(Name, Check());
            }
            catch (Exception x)
            {
                return new Result(Name, HealthCheckResult.Unhealthy(x));
            }
        }

        private readonly Func<HealthCheckResult> check;
    }
}