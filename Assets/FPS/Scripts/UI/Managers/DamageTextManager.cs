using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;

namespace Unity.FPS.UI
{
    public class DamageTextManager : Singleton<DamageTextManager>
    {
        private const string PLAYER_TAG = "Player";
        private const int POPUP_POOL_INITIAL_SIZE = 20;

        private bool hasFinishedLoading;
        private GameObject damageTextPopupPrefab;

        private Queue<DamageReport> unprocessedDamageReports = new Queue<DamageReport>();
        private List<Popup> currentPopups = new List<Popup>();

        [SerializeField] private RectTransform damageTextParent;


        private void Start()
        {
            Health.OnSomeoneDamaged += ReportDamage;

            StartCoroutine(LoadAssets());
        }

        private IEnumerator LoadAssets()
        {
            yield return Addressables.LoadAssetsAsync<GameObject>("DamageTextPopup", (GameObject loadedObject) =>
            {
                damageTextPopupPrefab = loadedObject;
            }).Task;

            PoolManager.Instance.warmPool(damageTextPopupPrefab, POPUP_POOL_INITIAL_SIZE);

            hasFinishedLoading = true;
        }

        public void ReportDamage(float damage, GameObject victim, GameObject source)
        {
            if (victim == null)
            {
                return;
            }

            if (source == null)
            {
                return;
            }

            if (source.tag != PLAYER_TAG)
            {
                return;
            }

            PlayerWeaponsManager playerWeaponsManager = source.GetComponent<PlayerWeaponsManager>();
            if (playerWeaponsManager == null)
            {
                Debug.LogError("Failed to find PlayerWeaponsManager!");
            }

            unprocessedDamageReports.Enqueue(
                new DamageReport(
                    victim,
                    Mathf.CeilToInt(damage),
                    playerWeaponsManager.GetActiveWeapon().WeaponIcon
                )
            );
        }

        private void Update()
        {
            if (!hasFinishedLoading)
            {
                return;
            }

            while (unprocessedDamageReports.Count > 0)
            {
                DamageReport report = unprocessedDamageReports.Dequeue();

                if (report.Victim == null)
                {
                    continue;
                }

                Popup popup = new Popup(damageTextPopupPrefab, damageTextParent, report);
                currentPopups.Add(popup);
            }

            for (int i = currentPopups.Count - 1; i >= 0; i--)
            {
                Popup popup = currentPopups[i];
                popup.Update(out bool isStillAlive);

                if (!isStillAlive)
                {
                    currentPopups.RemoveAt(i);
                }
            }
        }

        private struct DamageReport
        {
            public GameObject Victim;
            public int Damage;
            public Sprite WeaponSprite;

            public DamageReport(GameObject victim, int damage, Sprite weaponSprite)
            {
                Victim = victim;
                Damage = damage;
                WeaponSprite = weaponSprite;
            }
        }

        private class Popup
        {
            public const float LIFETIME = 1f;
            public const float MAX_OFFSET = 1f;
            public const float START_HEIGHT = 2f;

            public float TimeCreated;
            public Vector3 WorldPosToFollow;

            private GameObject gameObject;
            private RectTransform rectTransform;
            private Image image;
            private TextMeshProUGUI text;

            private float originalImageAlpha;
            private float originalTextAlpha;

            public Popup(GameObject prefab, Transform parent, DamageReport report)
            {
                TimeCreated = Time.time;
                WorldPosToFollow = report.Victim.transform.position;

                gameObject = PoolManager.Instance.spawnObject(prefab);
                rectTransform = gameObject.GetComponent<RectTransform>();
                image = gameObject.GetComponentInChildren<Image>();
                text = gameObject.GetComponentInChildren<TextMeshProUGUI>();

                originalImageAlpha = image.color.a;
                originalTextAlpha = text.color.a;

                rectTransform.SetParent(parent);

                image.sprite = report.WeaponSprite;
                text.text = report.Damage.ToString();
            }

            public void Update(out bool isStillAlive)
            {
                float progress = (Time.time - TimeCreated) / LIFETIME;

                float offsetY = Mathf.Lerp(0f, MAX_OFFSET, progress);
                rectTransform.position = Camera.main.WorldToScreenPoint(WorldPosToFollow + new Vector3(0f, START_HEIGHT + offsetY, 0f));

                float imageAlpha = Mathf.Lerp(originalImageAlpha, 0f, progress);
                image.color = new Color(image.color.r, image.color.g, image.color.b, imageAlpha);

                float textAlpha = Mathf.Lerp(originalTextAlpha, 0f, progress);
                text.color = new Color(text.color.r, text.color.g, text.color.b, textAlpha);

                isStillAlive = progress < 1f;

                if (!isStillAlive)
                {
                    PoolManager.Instance.releaseObject(gameObject);
                }
            }
        }
    }
}
