namespace EnumBenchmarkDemo
{
    using BenchmarkDotNet.Attributes;

    internal static class EnumExtensions
    {
        public static string FastToString(this TestEnum value)
        {
            switch (value)
            {
                case TestEnum.TestValue0:
                    return nameof(TestEnum.TestValue0);
                case TestEnum.TestValue1:
                    return nameof(TestEnum.TestValue1);
                case TestEnum.TestValue2:
                    return nameof(TestEnum.TestValue2);
                case TestEnum.TestValue3:
                    return nameof(TestEnum.TestValue3);
                case TestEnum.TestValue4:
                    return nameof(TestEnum.TestValue4);
                case TestEnum.TestValue5:
                    return nameof(TestEnum.TestValue5);
                case TestEnum.TestValue6:
                    return nameof(TestEnum.TestValue6);
                case TestEnum.TestValue7:
                    return nameof(TestEnum.TestValue7);
            }

            return value.ToString();
        }
    }

    internal enum TestEnum
    {
        TestValue0 = 0, 
        TestValue1,
        TestValue2,
        TestValue3,
        TestValue4,
        TestValue5,
        TestValue6,
        TestValue7,
        TestValue8,
    }

    [MemoryDiagnoser]
    public class EnumToStringTest
    {
        public EnumToStringTest()
        {
        }

        [Benchmark]
        [InvocationCount(10000000)]
        public string TestWithEnumHelper()
        {
             return EnumHelper<TestEnum>.ToString((long)TestEnum.TestValue0);
        }

        [Benchmark]
        [InvocationCount(10000000)]
        public string TestWithNameOf()
        {
            return nameof(TestEnum.TestValue0);
        }


        [Benchmark(Baseline = true)]
        [InvocationCount(10000000)]
        public string TestWithDefaultToString()
        {
            return TestEnum.TestValue0.ToString();
        }

        [Benchmark]
        [InvocationCount(10000000)]
        public string TestWithExtension()
        {
            return EnumExtensions.FastToString(TestEnum.TestValue0);
        }
    }
}
