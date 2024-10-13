namespace StorageShared.Models
{
    public class ReadMessageTypeResult
    {
        public byte RawValue { get; private set; }
        public MessageType? MessageType { get; private set; }
        public bool Valid { get; private set; }

        public ReadMessageTypeResult(byte rawValue)
        {
            RawValue = rawValue;

            MessageType type = (MessageType)rawValue;

            if (Enum.IsDefined(typeof(MessageType), type))
            {
                MessageType = type;
                Valid = true;
            }
        }
    }
}
