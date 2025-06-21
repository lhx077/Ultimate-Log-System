namespace UltimateLogSystem
{
    /// <summary>
    /// 自定义日志级别
    /// </summary>
    public class CustomLogLevel
    {
        public int Value { get; }
        public string Name { get; }
        
        private CustomLogLevel(int value, string name)
        {
            Value = value;
            Name = name;
        }
        
        /// <summary>
        /// 创建自定义日志级别
        /// </summary>
        public static CustomLogLevel Create(int value, string name)
        {
            return new CustomLogLevel(value, name);
        }
        
        public override string ToString() => Name;
    }
} 