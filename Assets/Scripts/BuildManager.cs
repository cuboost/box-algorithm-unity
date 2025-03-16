using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BuildManager : MonoBehaviour
{
    public GameObject rodA, rodB, rodC;
    public GameObject boxPrefab; // Prefab for disks
    private Stack<Transform>[] rods;
    int numBoxes = 5;
    public TMP_InputField inputField;
    public Material[] diskMaterials;
    float baseSize = 2f; // Size of the largest (bottom) disk
    float sizeStep = 0.2f; // How much each disk shrinks
    private Coroutine simulationCoroutine;
    private Transform movingDisk;
    public TextMeshPro stepsText;
    public ParticleSystem confettiParticleSystem;

    void Start()
    {
        rods = new Stack<Transform>[3];
        rods[0] = new Stack<Transform>();
        rods[1] = new Stack<Transform>();
        rods[2] = new Stack<Transform>();
        // Set default value
        inputField.text = numBoxes.ToString();
        inputField.onEndEdit.AddListener(delegate { StartSimulation(); });

        // Start simulation with default value
        StartSimulation();
    }

    public void StartSimulation()
    {
        int oldNumBoxes = numBoxes;
        if (int.TryParse(inputField.text, out numBoxes))
        {
            if (numBoxes < 11 && numBoxes > 1)
            {
                ResetSimulation();
                if (simulationCoroutine != null)
                {
                    StopCoroutine(simulationCoroutine); // Stop the running simulation
                }
                simulationCoroutine = StartCoroutine(GenerateDisks(numBoxes)); // Start a new simulation
            }
            else
            {
                Debug.LogError("Invalid number of boxes: Number is not between 2 and 10");
                numBoxes = oldNumBoxes;
                inputField.text = numBoxes.ToString();
            }
        }
        else
        {
            Debug.LogError("Invalid number of boxes");
        }
    }
    private void ResetSimulation()
    {
        // Stop the running simulation
        if (simulationCoroutine != null)
        {
            StopCoroutine(simulationCoroutine);
            simulationCoroutine = null;
        }

        // Destroy the moving disk if any
        if (movingDisk != null)
        {
            Destroy(movingDisk.gameObject);
            movingDisk = null;
        }

        // Clear the rods and destroy the disks
        for (int i = 0; i < rods.Length; i++)
        {
            while (rods[i].Count > 0)
            {
                Transform disk = rods[i].Pop();
                Destroy(disk.gameObject);
            }
        }

        // Clear the steps text
        if (stepsText != null)
        {
            stepsText.text = "";
        }
    }
    IEnumerator GenerateDisks(int n)
    {
        for (int i = 0; i < n; i++)
        {
            string stepMessage = $"Création de la boîte {i + 1}";
            Debug.Log(stepMessage);
            if (stepsText != null)
            {
                stepsText.text = stepMessage;
            }
            GameObject disk = Instantiate(boxPrefab);
            sizeStep = baseSize / numBoxes;
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
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(SolveHanoi(numBoxes, 0, 2, 1));

        // Display completion message
        if (stepsText != null)
        {
            stepsText.text = "Algorithme terminé!";
        }
        if (confettiParticleSystem != null)
        {
            confettiParticleSystem.Play();
        }
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
        movingDisk = disk;
        Rigidbody rb = disk.GetComponent<Rigidbody>();

        string stepMessage = $"Déplacement de la boîte de la position {from + 1} à la position {to + 1}";
        Debug.Log(stepMessage);
        if (stepsText != null)
        {
            stepsText.text = stepMessage;
        }

        float liftHeight = numBoxes * disk.transform.localScale.y + 0.3f; // How high the disk lifts
        float moveDuration = 0.5f; // Faster movement
        float dropSpeed = 0.3f; // Controlled drop speed

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
