using UnityEngine;

namespace CastingShouldBeFree.Core;

public class ThemeInfo : MonoBehaviour
{
    public enum Theme
    {
        Main,
        Secondary,
        Third,
    }
    
    public Theme ThemeInformation;
}