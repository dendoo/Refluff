using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CarpetCleaning
{
    /// <summary>
    /// The letter a hotkey chip shows (Docs/LOCALIZATION.md 5). The game reads keys by their US position (Keyboard.current.zKey),
    /// so on a German QWERTZ keyboard the moisture lamp is the key printed "Y", on a French AZERTY one the key printed "W".
    /// The chips show the letter the player's layout prints there. A layout that prints no Latin letter there (Cyrillic,
    /// Greek, Arabic...) keeps the US letter: those keyboards carry the Latin legend too.
    /// </summary>
    public static class KeyNames
    {
        public static string Label(Key key)
        {
            string us = UsLetter(key);
            var kb = Keyboard.current;
            if (kb == null || us == null) return us ?? key.ToString();
            KeyControl control;
            try { control = kb[key]; }
            catch (System.ArgumentOutOfRangeException) { return us; }
            string shown = control != null ? control.displayName : null;
            if (string.IsNullOrEmpty(shown) || shown.Length != 1) return us;
            char c = char.ToUpperInvariant(shown[0]);
            return c >= 'A' && c <= 'Z' ? c.ToString() : us;
        }

        /// <summary>The Key of a US letter ("Z" -> Key.Z); Key.None when `letter` is not A-Z.</summary>
        public static Key FromLetter(string letter)
        {
            if (string.IsNullOrEmpty(letter) || letter.Length != 1) return Key.None;
            char c = char.ToUpperInvariant(letter[0]);
            if (c < 'A' || c > 'Z') return Key.None;
            return Key.A + (c - 'A');
        }

        static string UsLetter(Key key)
        {
            if (key < Key.A || key > Key.Z) return null;
            return ((char)('A' + (key - Key.A))).ToString();
        }
    }
}
