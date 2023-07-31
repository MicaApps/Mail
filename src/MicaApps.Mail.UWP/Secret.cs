namespace Mail
{
    internal static class Secret
    {
#if DEBUG
        public const string AadClientId = "0b3dac55-dc21-442b-ace7-ccefbb5a9f80";
#else
        public const string AadClientId = "<Mail-AAD-Secret-Value>";
#endif
    }
}