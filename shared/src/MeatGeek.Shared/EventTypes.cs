namespace MeatGeek.Shared
{
    public static class EventTypes
    {

        public static class Sessions
        {
            public const string SessionCreated = nameof(SessionCreated);
            public const string SessionDeleted = nameof(SessionDeleted);
            public const string SessionUpdated = nameof(SessionUpdated);
            public const string SessionEnded = nameof(SessionEnded);
        }

        // public static class Images
        // {
        //     public const string ImageCaptionUpdated = nameof(ImageCaptionUpdated);
        //     public const string ImageCreated = nameof(ImageCreated);
        //     public const string ImageDeleted = nameof(ImageDeleted);
        // }

    }
}
