using UnityEngine;

public class DrawAxes : MonoBehaviour
{
    public float axisLength = 5f;
    public float axisThickness = 0.05f;

    void Start()
    {
        // Create parent object at origin
        GameObject axesParent = new GameObject("3D_Coordinate_Axes");
        axesParent.transform.position = Vector3.zero;

        // X-Axis (Red)
        CreateAxisLine("X_Axis_Red", Vector3.right, Color.red, axesParent.transform);
        
        // Y-Axis (Green)
        CreateAxisLine("Y_Axis_Green", Vector3.up, Color.green, axesParent.transform);
        
        // Z-Axis (Blue)
        CreateAxisLine("Z_Axis_Blue", Vector3.forward, Color.blue, axesParent.transform);
    }

    void CreateAxisLine(string name, Vector3 direction, Color color, Transform parent)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.parent = parent;

        // Position at half length along direction so it starts at origin
        cylinder.transform.position = direction * (axisLength / 2f);
        cylinder.transform.localScale = new Vector3(axisThickness, axisLength / 2f, axisThickness);
        cylinder.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

        // Remove collider
        Destroy(cylinder.GetComponent<Collider>());

        // Color material
        Renderer renderer = cylinder.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.color = color;
    }
}
