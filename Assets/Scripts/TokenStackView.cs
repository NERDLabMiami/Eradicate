using System.Collections.Generic;
using UnityEngine;

namespace Eradicate
{
    public class TokenStackView : MonoBehaviour
    {
        [SerializeField] private Transform tokenParent;   // assign or leave null to use self
        [SerializeField] private GameObject tokenPrefab;

        private readonly List<GameObject> _spawned = new();

        private void Awake()
        {
            if (tokenParent == null) tokenParent = transform;
        }

        public void SetTokenPrefab(GameObject prefab) => tokenPrefab = prefab;

        public void SetCount(int count)
        {
            if (tokenPrefab == null)
            {
                Debug.LogError($"{name}: TokenStackView missing tokenPrefab.");
                return;
            }

            count = Mathf.Max(0, count);

            while (_spawned.Count < count)
                _spawned.Add(Instantiate(tokenPrefab, tokenParent));

            while (_spawned.Count > count)
            {
                int last = _spawned.Count - 1;
                var go = _spawned[last];
                _spawned.RemoveAt(last);
                if (go != null) Destroy(go);
            }
        }
    }
}