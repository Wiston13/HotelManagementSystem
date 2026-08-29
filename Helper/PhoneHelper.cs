namespace HotelManagementSystem.Helper
{
    public static class PhoneHelper
    {
        public static bool TryNormalize(string? input, out string normalizedPhone)
        {
            normalizedPhone = string.Empty;

            if (input == null)
            {
                return false;
            }

            var characters = new List<char>(input.Length);

            foreach (var character in input)
            {
                if (!char.IsWhiteSpace(character) && character != '-')
                {
                    characters.Add(character);
                }
            }

            normalizedPhone = new string(characters.ToArray());

            return normalizedPhone.Length > 0 &&
                   normalizedPhone.Length <= 20 &&
                   normalizedPhone.All(character => character >= '0' && character <= '9');
        }
    }
}
