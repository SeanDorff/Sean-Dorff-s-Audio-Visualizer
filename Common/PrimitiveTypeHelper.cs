namespace Common
{
    public static class PrimitiveTypeHelper
    {
        public static int IntValue(EPrimitiveType ePrimitiveType) => (int)ePrimitiveType;
        public static string StringValue(EPrimitiveType ePrimitiveType) => ePrimitiveType.ToString();
    }
}
