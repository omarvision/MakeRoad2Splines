using System.Xml.Serialization;
using TMPro;
using UnityEngine;

public class CarController : MonoBehaviour
{
    #region --- Variables ---
    [Header("[Other Scene Object References]")]
    public TextMeshProUGUI txtMPH = null;
    
    [Header("[Car Controls]")]
    public float MoveSpeed = 24;
    public float TurnSpeed = 90;
    
    [Header("[Gravity Scaling]")]
    [Tooltip("How much faster than normal gravity do we drop? gives car a sense of weight in game.")]
    public float GravMultiplier = 3;

    [Tooltip("At what drop velocity do we start the gravity scaling? Too low a value and the car may be slow to move.")]
    public float dropThreshold = -1.5f; 
    public bool doGravityScaling = true;

    private Rigidbody rb = null;
    private float raycast_down_distance;
    private const float baseGravity = 9.81f;
    private Vector3 localVelocity;
    #endregion

    private void Start()
    {
        rb = this.GetComponent<Rigidbody>();
        raycast_down_distance = this.GetComponent<Renderer>().bounds.size.y * 0.515f;
    }
    private void FixedUpdate()
    {
        //instead of worldspace velocity, what is the velocity
        //with respect to gameobject's transform
        localVelocity = this.transform.InverseTransformDirection(rb.velocity);

        CarMoveControls();
        GravityDownIncrease();
        DashboardUpdate();
    }
    private void CarMoveControls()
    {
        if (IsCarDrivable() == true)
        {
            if (Input.GetKey(KeyCode.UpArrow) == true)
            {
                rb.AddRelativeForce(Vector3.forward * MoveSpeed * Time.deltaTime, ForceMode.VelocityChange);
            }
            if (Input.GetKey(KeyCode.DownArrow) == true)
            {
                rb.AddRelativeForce(Vector3.forward * -(MoveSpeed * 0.5f) * Time.deltaTime, ForceMode.VelocityChange);
            }

            if (Input.GetKey(KeyCode.LeftArrow) == true)
            {
                rb.MoveRotation(rb.rotation * Quaternion.Euler(Vector3.up * -((rb.velocity.magnitude + TurnSpeed) * Time.deltaTime)));
            }
            if (Input.GetKey(KeyCode.RightArrow) == true)
            {
                rb.MoveRotation(rb.rotation * Quaternion.Euler(Vector3.up * ((rb.velocity.magnitude + TurnSpeed) * Time.deltaTime)));
            }
        }
    }
    private bool IsCarDrivable()
    {
        //Purpose:
        //  raycast down from the center of the car. Why?
        //  to see if the car's wheels are on the road. If
        //  the wheels are on the road then we can let the user
        //  control the car.

        bool ret = false;

        //check at least 2 wheels on ground
        RaycastHit hit;
        if (Physics.Raycast(this.transform.position, Vector3.down, out hit, raycast_down_distance) == true)
            ret = true;

        return ret;
    }
    private void GravityDownIncrease()
    {
        if (doGravityScaling == false)
            return;

        // Check if the object is falling (fast enough beyond threshold)
        if (localVelocity.y <= dropThreshold)
        {
            //accelerate the fall even faster
            float increasedGravity = baseGravity * GravMultiplier;
            rb.AddForce(Vector3.down * increasedGravity, ForceMode.Acceleration);

            Debug.Log(string.Format("gravity: {0:F2}", rb.velocity.y));
        }
    }
    private void DashboardUpdate()
    {
        txtMPH.text = string.Format("MPH {0:F0}", localVelocity.z * 2.5f);
    }   
}
