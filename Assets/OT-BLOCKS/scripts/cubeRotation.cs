using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
//using Microsoft.Unity.VisualStudio.Editor;

public class cubeRotation : MonoBehaviour
{
    int currentFace;
    internal float rotation;
    [SerializeField]
    Transform cubeTransform;
    [SerializeField]
    float speed, rotationSpeed = 1f;
    [SerializeField]
    float jumpPower = 2f;
    [SerializeField]
    int jumpCount = 1;
    float zPosition;
    [SerializeField] float totalDuration;
    [SerializeField] private Image AnimImage;

    [SerializeField] private List<Image> AnimationObject = new List<Image>();
    void Start()
    {


        //Quaternion targetRotation = Quaternion.Euler(0, 90, 0);
        //StartRotation(targetRotation);
        zPosition = cubeTransform.position.z;
    }

    internal void StartRotation(Vector3 rotate, float duration)
    {
        totalDuration = duration;

        // cubeTransform.DOLocalRotate(rotate, rotationSpeed);
        // cubeTransform.DOMoveZ(
        //     cubeTransform.position.z - 5f,speed).SetEase(Ease.OutQuad).OnComplete(() => {cubeTransform.DOMoveZ(zPosition,speed).SetEase(Ease.InQuad);});

        float rotationTime = totalDuration * 0.3f;
        float moveForwardTime = totalDuration * 0.35f;
        float moveBackwardTime = totalDuration * 0.35f;

        // do the sequence
        Sequence seq = DOTween.Sequence();
        seq.Append(cubeTransform.DOLocalRotate(rotate, rotationTime))
           .Join(cubeTransform.DOMoveZ(cubeTransform.position.z - 7f, moveForwardTime).SetEase(Ease.OutQuad))
           .Append(cubeTransform.DOMoveZ(zPosition, moveBackwardTime).SetEase(Ease.InQuad));

    }

    internal void EnableAnimObjects()
    {
        Debug.Log($"Win animation enable 1");
       foreach (Image img in AnimationObject)
        {
            // Color c = img.color;
            // c.a = 0f;
            img.gameObject.SetActive(true);
        }
    }

    internal void ResetAnimObject()
    {
        foreach (Image img in AnimationObject)
        {
            // Color c = img.color;
            // c.a = 0f;
            img.gameObject.SetActive(false);
        }
    }
    
}
