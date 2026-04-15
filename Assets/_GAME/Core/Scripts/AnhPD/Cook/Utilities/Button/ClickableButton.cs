// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Events;
// using Utilities;

// namespace AnhPD.Cook
// {
//   public class ClickableButton : MonoBehaviour
//   {
//     [SerializeField] private SpriteRenderer on, off;
//     [SerializeField] private Collider2D coll2D;
//     [SerializeField] private AudioClip sfxClick;
//     public bool isReady;
//     public UnityEvent onClick;

//     private void OnMouseDown()
//     {
//       if (!LevelBase.Ins.IsAllowInteract) return;
//       StopAllCoroutines();
//       // AudioManager.PlaySFX(sfxClick);
//       on.enabled = true;
//       off.enabled = false;
//       if (!isReady)
//       {
//         this.WaitToDo((() =>
//         {
//           on.enabled = false;
//           off.enabled = true;
//         }), .15f);
//       }
//       else
//       {
//         onClick?.Invoke();
//         isReady = false;
//         coll2D.enabled = false;
//       }
//     }

//     public void OnReady()
//     {
//       isReady = true;
//     }

//     public void OnReset()
//     {
//       coll2D.enabled = true;

//       on.enabled = false;
//       off.enabled = true;
//     }
//   }
// }
