namespace EnumBenchmarkDemo
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Engines;
    using Microsoft.Diagnostics.Runtime.Utilities;

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
        public int TestWithEnumHelper()
        {
            int result = 1000;
           
            for(int i = 0; i< 1000000; i++)
            {
                var test = EnumHelper<TestEnum>.ToString((long)TestEnum.TestValue7);
                result += HandleHashCode(test, i) % 3;
            }

            return result;
        }

        [Benchmark]
        public int TestWithNameOf()
        {
            int result = 1000;
            for (int i = 0; i < 1000000; i++)
            {
                var test = nameof(TestEnum.TestValue7);
                result += HandleHashCode(test, i) % 3;
            }

            return result;
        }


        [Benchmark]
        public int TestWithDefaultToString()
        {
            int result = 1000;
            for (int i = 0; i < 1000000; i++)
            {
                var test = TestEnum.TestValue7.ToString();
                result += HandleHashCode(test, i) %3;
            }

            return result;
        }

        private int HandleHashCode(string test, int i)
        {
            return test.GetHashCode() + i;
        }
    }
}
