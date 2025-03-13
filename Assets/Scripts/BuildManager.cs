using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public GameObject rodA, rodB, rodC;
    public GameObject boxPrefab; // Prefab for disks
    private Stack<Transform>[] rods;
    private int numBoxes;
    float baseSize = 2f; // Size of the largest (bottom) disk
    float sizeStep = 0.2f; // How much each disk shrinks

    void Start()
    {
        rods = new Stack<Transform>[3];
        rods[0] = new Stack<Transform>();
        rods[1] = new Stack<Transform>();
        rods[2] = new Stack<Transform>();
        StartSimulation();
    }

    public void StartSimulation()
    {
        // numBoxes = int.Parse(inputField.text);
        numBoxes = 10;
        StartCoroutine(GenerateDisks(numBoxes));
    }

    public Material[] diskMaterials;
    IEnumerator GenerateDisks(int n)
    {
        for (int i = 0; i < n; i++)
        {
            Debug.Log("Creating disk " + i);
            GameObject disk = Instantiate(boxPrefab);
            float scaleFactor = baseSize - (i * sizeStep);
            disk.transform.localScale = new Vector3(scaleFactor, 0.3f, scaleFactor);
            float height = i + 2f;
            disk.transform.position = new Vector3(rodA.transform.position.x, height, rodA.transform.position.z);
            // Assign different materials based on index
            Renderer diskRenderer = disk.GetComponent<Renderer>();
            if (diskRenderer != null && diskMaterials.Length > 0)
            {
                diskRenderer.material = diskMaterials[i % diskMaterials.Length]; // Loops through materials
            }

            rods[0].Push(disk.transform);
            yield return new WaitForSeconds(1f);
        }
        yield return new WaitForSeconds(1f);
        StartCoroutine(SolveHanoi(numBoxes, 0, 2, 1));
    }

    IEnumerator SolveHanoi(int n, int source, int destination, int auxiliary)
    {
        Debug.Log($"SolveHanoi called with n={n}, source={source}, destination={destination}, auxiliary={auxiliary}");


        if (n == 1)
        {
            yield return MoveDisk(source, destination);
            yield break;
        }
        yield return StartCoroutine(SolveHanoi(n - 1, source, auxiliary, destination));
        yield return MoveDisk(source, destination);
        yield return StartCoroutine(SolveHanoi(n - 1, auxiliary, destination, source));
    }

    IEnumerator MoveDisk(int from, int to)
    {
        if (rods[from].Count == 0) yield break;

        Transform disk = rods[from].Pop();
        Rigidbody rb = disk.GetComponent<Rigidbody>();

        Debug.Log($"Moving disk from rod {from} to rod {to}");

        float liftHeight = numBoxes * disk.transform.localScale.y + 0.3f; // How high the disk lifts
        float moveDuration = 1f; // Faster movement
        float dropSpeed = 0.5f; // Controlled drop speed

        Vector3 startPos = disk.position;
        Vector3 liftPos = new Vector3(startPos.x, liftHeight, startPos.z);
        Vector3 movePos = new Vector3(
            to == 0 ? rodA.transform.position.x : to == 1 ? rodB.transform.position.x : rodC.transform.position.x,
            liftHeight,
            rodA.transform.position.z
        );

        Vector3 dropPos = new Vector3(movePos.x, rods[to].Count * disk.transform.localScale.y + 0.4f, movePos.z); // Target drop position

        // 🚀 Step 1: Disable physics while lifting & moving
        rb.isKinematic = true;

        // Phase 1: Lift the disk
        yield return MoveToPosition(disk, liftPos, moveDuration);

        // Phase 2: Move in the air
        yield return MoveToPosition(disk, movePos, moveDuration);

        // 🚀 Step 2: Drop smoothly using Lerp instead of physics
        float elapsedTime = 0f;
        Vector3 startDropPos = disk.position;

        while (elapsedTime < dropSpeed)
        {
            disk.position = Vector3.Lerp(startDropPos, dropPos, elapsedTime / dropSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final landing position is accurate
        disk.position = dropPos;

        // Step 3: Reactivate physics after the drop is complete
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Add disk to the new rod stack
        rods[to].Push(disk);

        yield return new WaitForSeconds(0.3f); // Small delay before the next move
    }

    // Helper Coroutine for smooth movement
    IEnumerator MoveToPosition(Transform obj, Vector3 target, float duration)
    {
        float elapsedTime = 0f;
        Vector3 startPos = obj.position;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            t = Mathf.SmoothStep(0, 1, t); // Applies acceleration & deceleration

            obj.position = Vector3.Lerp(startPos, target, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        obj.position = target; // Ensure final position is exact
    }


}
