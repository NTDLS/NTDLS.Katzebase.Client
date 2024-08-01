﻿namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbActionResponseCollection : KbBaseActionResponse
    {
        public List<KbBaseActionResponse> Collection { get; set; } = new();

        private bool _success = true;
        public new bool Success
        {
            get => _success && Collection.All(o => o.Success);
            set => _success = value;
        }

        public KbActionResponseCollection()
        {
        }

        public KbActionResponseCollection(Exception ex)
        {
            _success = false;
            ExceptionText = ex.Message;
        }

        public void Add(KbBaseActionResponse result)
        {
            Collection.Add(result);
        }
    }
}
