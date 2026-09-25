using UnityEngine;

namespace BeaverCreek
{
    public sealed class CreekWind : MonoBehaviour
    {
        void OnEnable()
        {
            Shader.SetGlobalFloat("WindPower", .12f);
            Shader.SetGlobalFloat("WindSpeed", .5f);
            Shader.SetGlobalFloat("WindBurstsPower", .12f);
            Shader.SetGlobalFloat("WindBurstsSpeed", 1.2f);
            Shader.SetGlobalFloat("WindBurstsScale", 20f);
            Shader.SetGlobalFloat("MicroPower", .06f);
            Shader.SetGlobalFloat("MicroSpeed", .65f);
            Shader.SetGlobalFloat("MicroFrequency", 2f);
            bool mobile = Application.isMobilePlatform || (Application.platform == RuntimePlatform.WebGLPlayer && Screen.width <= 700);
            Shader.SetGlobalFloat("GrassRenderDist", mobile ? 58f : 110f);
        }
    }
}
