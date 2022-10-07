using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolygonMerger : MonoBehaviour
{
    private List<Vector2> polygon1;
    private List<Vector2> polygon2;
    private List<Vector2> polygon3;

    public GameObject polygonObject1;
    public GameObject polygonObject2;
    public GameObject polygonObject3;
    public GameObject polygonObject4;

    private bool merged = false;

    // Start is called before the first frame update
    private void Start()
    {
        polygon1 = new List<Vector2>();
        polygon2 = new List<Vector2>();
        polygon3 = new List<Vector2>();

        polygon1.AddRange(new List<Vector2> { new Vector2(4, 13), new Vector2(6, 15), new Vector2(10, 15), new Vector2(8, 10), new Vector2(6, 9) });
        polygon2.AddRange(new List<Vector2> { new Vector2(7, 12), new Vector2(12, 13), new Vector2(16, 10), new Vector2(15, 5), new Vector2(10, 4) });
        polygon3.AddRange(new List<Vector2> { new Vector2(6, 1), new Vector2(9, -2), new Vector2(5, -6), new Vector2(4, -2)});

        DrawNewPolygonMesh(polygon1, polygonObject1);
        DrawNewPolygonMesh(polygon2, polygonObject2);
        DrawNewPolygonMesh(polygon3, polygonObject3);
    }

    // Update is called once per frame
    private void Update()
    {
        //When pressed space, the polygons will merge
        if (Input.GetKeyUp("space") && !merged)
        {
            merged = true;
            Debug.Log("Merging the polygons");

            List<Vector2>[] polygonsArray = new List<Vector2>[2];
            polygonsArray[0] = polygon1;
            polygonsArray[1] = polygon2;
            List<Vector2> newUnion = FindPolygonUnion(polygonsArray);

            polygonObject1.SetActive(false);
            polygonObject2.SetActive(false);
            DrawNewPolygonMesh(newUnion, polygonObject4);
        }
    }


    public void DrawNewPolygonMesh(List<Vector2> uvPolygons, GameObject polygonObject)
    {
        //Since Unity mesh can only be created from array of UV, convert list to array
        Vector2[] uvArray = uvPolygons.ToArray();

        LineRenderer lineRender = polygonObject.GetComponent<LineRenderer>();
        lineRender.positionCount = uvArray.Length;
        for (int i = 0; i < uvArray.Length; i++)
        {
            lineRender.SetPosition(i, uvArray[i]);
        }
        lineRender.loop = true;
    }

    /// <summary>
    /// Returns a union of polygons
    /// </summary>
    /// <param name="polygons">Insert an array of polygons to join</param>
    /// <returns></returns>
    private List<Vector2> FindPolygonUnion(List<Vector2>[] polygons)
    {
        // Find the lower-leftmost point in either polygon.
        int currentPolygon = 0;
        int currentIndex = 0;
        Vector2 currentPoint = polygons[currentPolygon][currentIndex];
        for (int pgon = 0; pgon < 2; pgon++)
        {
            for (int index = 0; index < polygons[pgon].Count; index++)
            {
                Vector2 testPoint = polygons[pgon][index];
                if ((testPoint.x < currentPoint.x) ||
                    ((testPoint.x == currentPoint.x) &&
                     (testPoint.y > currentPoint.y)))
                {
                    currentPolygon = pgon;
                    currentIndex = index;
                    currentPoint = polygons[currentPolygon][currentIndex];
                }
            }
        }

        // Create the result polygon.
        List<Vector2> union = new List<Vector2>();

        // Start here.
        Vector2 startingPoint = currentPoint;
        union.Add(startingPoint);

        // Start traversing the polygons.
        // Repeat until we return to the starting point.
        while(true)
        {
            // Find the next point.
            int nextIndex = (currentIndex + 1) % polygons[currentPolygon].Count;
            Vector2 nextPoint = polygons[currentPolygon][nextIndex];

            // Each time through the loop:
            //      cur_pgon is the index of the polygon we're following
            //      cur_point is the last point added to the union
            //      next_point is the next point in the current polygon
            //      next_index is the index of next_point

            // See if this segment intersects
            // any of the other polygon's segments.
            int otherPolygon = (currentPolygon + 1) % 2;

            // Keep track of the closest intersection.
            Vector2 bestIntersection = new Vector2(0, 0);
            int bestIndex1 = -1;
            float bestT = 2f;

            for (int index1 = 0; index1 < polygons[otherPolygon].Count; index1++)
            {
                // Get the index of the next point in the polygon.
                int index2 = (index1 + 1) % polygons[otherPolygon].Count;

                // See if the segment between points index1
                // and index2 intersect the current segment.
                Vector2 point1 = polygons[otherPolygon][index1];
                Vector2 point2 = polygons[otherPolygon][index2];
                bool intersectedLines;
                bool intersectedSegments;
                Vector2 intersection; 
                Vector2 closePoint1; 
                Vector2 closePoint2;
                float t1; 
                float t2;
                FindIntersection(currentPoint, nextPoint, point1, point2,
                    out intersectedLines, out intersectedSegments,
                    out intersection, out closePoint1, out closePoint2, out t1, out t2);

                if ((intersectedSegments) && // The segments intersect
                    (t1 > 0.001) &&         // Not at the previous intersection
                    (t1 < bestT))          // Better than the last intersection found
                {
                    // See if this is an improvement.
                    if (t1 < bestT)
                    {
                        // Save intersection.
                        bestT = t1;
                        bestIndex1 = index1;
                        bestIntersection = intersection;
                    }
                }
            }

            // See if any intersections found.
            if (bestT < 2f)
            {
                // Found an intersection.
                union.Add(bestIntersection);

                // Prepare to search for the next point from here.
                // Start following the other polygon.
                currentPolygon = (currentPolygon + 1) % 2;
                currentPoint = bestIntersection;
                currentIndex = bestIndex1;
            }
            else
            {
                // We didn't find an intersection.
                // Move to the next point in this polygon.
                currentPoint = nextPoint;
                currentIndex = nextIndex;

                // If we've returned to the starting point, we're done.
                if (currentPoint == startingPoint) break;

                // Add the current point to the union.
                union.Add(currentPoint);
            }
        }
        return union;
    }

    private void FindIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4,
            out bool intersectedLines, out bool intersectedSegments,
            out Vector2 intersection, out Vector2 closePoint1, out Vector2 closePoint2,
            out float t1, out float t2)
    {
        // Get the segments' parameters.
        float dx12 = p2.x - p1.x;
        float dy12 = p2.y - p1.y;
        float dx34 = p4.x - p3.x;
        float dy34 = p4.y - p3.y;

        // Solve for t1 and t2
        float denominator = (dy12 * dx34 - dx12 * dy34);
        t1 = ((p1.x - p3.x) * dy34 + (p3.y - p1.y) * dx34) / denominator;
        if (float.IsInfinity(t1))
        {
            // The lines are parallel (or close enough to it).
            intersectedLines = false;
            intersectedSegments = false;
            intersection = new Vector2(float.NaN, float.NaN);
            closePoint1 = new Vector2(float.NaN, float.NaN);
            closePoint2 = new Vector2(float.NaN, float.NaN);
            t2 = float.PositiveInfinity;
            return;
        }
        intersectedLines = true;

        t2 = ((p3.x - p1.x) * dy12 + (p1.y - p3.y) * dx12) / -denominator;

        // Find the point of intersection.
        intersection = new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1);

        // The segments intersect if t1 and t2 are between 0 and 1.
        intersectedSegments = ((t1 >= 0) && (t1 <= 1) && (t2 >= 0) && (t2 <= 1));

        // Find the closest points on the segments.
        if (t1 < 0) t1 = 0;
        else if (t1 > 1) t1 = 1;

        if (t2 < 0) t2 = 0;
        else if (t2 > 1) t2 = 1;

        closePoint1 = new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1);
        closePoint2 = new Vector2(p3.x + dx34 * t2, p3.y + dy34 * t2);
    }
}
