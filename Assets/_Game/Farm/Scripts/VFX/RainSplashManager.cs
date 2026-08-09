using UnityEngine;
using Day_Night;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RainSplashManager : MonoBehaviour
{
    [Header("Settings")]
    public int splashesPerSecond = 10;
    public Vector2 spawnArea = new Vector2(20f, 12f);
    public float splashScale = 1.5f;

    [Header("Sprite")]
    public Sprite[] splashSprites;
    public float animSpeed = 15f;
    
    private DayNightWeatherSystem weatherSystem;
    private float timer;

    void Start()
    {
        weatherSystem = FindObjectOfType<DayNightWeatherSystem>();
        
#if UNITY_EDITOR
        if (splashSprites == null || splashSprites.Length == 0)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Day_Night/VFX/Rain/RainSplashFlipbook.png");
            if (tex != null)
            {
                Object[] data = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(tex));
                System.Collections.Generic.List<Sprite> list = new System.Collections.Generic.List<Sprite>();
                foreach (var obj in data)
                {
                    if (obj is Sprite) list.Add((Sprite)obj);
                }
                splashSprites = list.ToArray();
            }
        }
#endif
    }

    void Update()
    {
        if (weatherSystem != null && weatherSystem.CurrentWeather != DayNightWeatherType.Rain)
            return;

        if (splashSprites == null || splashSprites.Length == 0)
            return;

        timer += Time.deltaTime;
        float spawnInterval = 1f / splashesPerSecond;

        while (timer >= spawnInterval)
        {
            timer -= spawnInterval;
            SpawnSplash();
        }
    }

    void SpawnSplash()
    {
        Vector3 camPos = Camera.main != null ? Camera.main.transform.position : transform.position;
        Vector3 randomPos = new Vector3(
            camPos.x + Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
            camPos.y + Random.Range(-spawnArea.y / 2, spawnArea.y / 2),
            0
        );

        GameObject splashObj = new GameObject("Splash");
        splashObj.transform.position = randomPos;
        splashObj.transform.localScale = new Vector3(splashScale, splashScale, 1f);

        SpriteRenderer sr = splashObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 300;
        sr.color = new Color(1f, 1f, 1f, 0.7f);

        SimpleSpriteAnimator anim = splashObj.AddComponent<SimpleSpriteAnimator>();
        anim.sprites = splashSprites;
        anim.fps = animSpeed;
        anim.destroyOnEnd = true;
    }
}

public class SimpleSpriteAnimator : MonoBehaviour
{
    public Sprite[] sprites;
    public float fps = 15f;
    public bool destroyOnEnd = true;

    private SpriteRenderer sr;
    private float timer;
    private int frame;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sprites != null && sprites.Length > 0)
            sr.sprite = sprites[0];
    }

    void Update()
    {
        if (sprites == null || sprites.Length == 0) return;
        
        timer += Time.deltaTime;
        float frameTime = 1f / fps;
        
        if (timer >= frameTime)
        {
            timer -= frameTime;
            frame++;
            if (frame >= sprites.Length)
            {
                if (destroyOnEnd)
                    Destroy(gameObject);
                else
                    frame = 0;
            }
            else
            {
                sr.sprite = sprites[frame];
            }
        }
    }
}
