#version 330 core
in vec3 screenCoord;
out vec4 FragColor;

//screen space is ([0,1], [0,1], z in vertex shader)
//all objects in screen space, zNear is -1
vec3 camPos = vec3(0.5, 0.5, 0.0);

//ray
struct Ray {
    vec3 origin;
    vec3 direction;
}; 

Ray RayConstructor(vec3 origin, vec3 direction)
{
	Ray ray;
	ray.origin = origin;
	ray.direction = normalize(direction);
	return ray;
}

//sphere
struct Sphere 
{
    vec3 center;
    float radius;
	vec3 color;
}; 

Sphere SphereConstructor(vec3 center, float radius, vec3 color)
{
	Sphere sphere;
	sphere.center = center;
	sphere.radius = radius;
	sphere.color = color;
	return sphere;
}

bool SphereHit(Sphere sphere, Ray ray, out float tnear)
{
	vec3 L = sphere.center - ray.origin;
	float tca = dot(L, ray.direction);
	float d2 = dot(L, L) - tca * tca;
	if (d2 > sphere.radius * sphere.radius) return false;
	float thc = sqrt(sphere.radius * sphere.radius - d2);
	tnear = tca - thc;
	float tfar = tca + thc;
	if (tnear < 0) tnear = tfar;
	return true;
}
struct Triangle
{
	vec3[3] points;
	vec3 color;
};
Triangle TriangleConstructor(vec3[3] points, vec3 color)
{
	Triangle triangle;
	triangle.points = points;
	triangle.color = color;
	return triangle;
}
bool InTriangle(Triangle triangle, Ray ray, out float tnear)
{
	bool isIn = false;
    vec3 E1 = triangle.points[1] - triangle.points[0]; //v1 - v0;
    vec3 E2 = triangle.points[2] - triangle.points[0]; //v2 - v0;
    vec3 S = ray.origin - triangle.points[0]; 
    vec3 S1 = cross(ray.direction, E2); //crossProduct(dir, E2);
    vec3 S2 = cross(S, E1); //crossProduct(S, E1);
    float coeff = 1.0 / dot(S1, E1);
    float t = coeff * dot(S2, E2);
    float b1 = coeff * dot(S1, S);
    float b2 = coeff * dot(S2, ray.direction);
    if (t >= 0 && b1 >= 0 && b2 >= 0 && (1 - b1 - b2) >= 0)
    {
        isIn = true;
        tnear = t;
        // u = b1;
        // v = b2;
    }
	return isIn;
}

void main()
{
	Ray ray = RayConstructor(camPos, screenCoord - camPos);

	Sphere sphere0 = SphereConstructor(vec3(0.6, 0.6, -2), 0.7, vec3(0.5, 0.1, 0.1));
	Sphere sphere1 = SphereConstructor(vec3(-0.3, -0.15, -2.6), 0.6, vec3(0.3, 0.3, 0.1));
	Triangle triangle0 = TriangleConstructor(vec3[](vec3(-3, -1, -1.1), vec3(2, -1, -1.1), vec3(-3, -1, -8)), vec3(0.1, 0.5, 0.1)); //left front, right front, left back
	Triangle triangle1 = TriangleConstructor(vec3[](vec3(2, -1, -1.1), vec3(2, -1, -8), vec3(-3, -1, -8)), vec3(0.1, 0.5, 0.1)); //right front, right back, left back

	Sphere spheres[2];
	spheres[0] = sphere0;
	spheres[1] = sphere1;
	Triangle triangles[2];
	triangles[0] = triangle0;
	triangles[1] = triangle1;

	
	//background color
	FragColor = vec4(0.1,0,0.2, 1.0);

	float distance = 999999;
	for(int i = 0; i < triangles.length(); i++)
	{
		float t;
		if (InTriangle(triangles[i], ray, t) && t < distance)
		{
			distance = t;
			FragColor = vec4(triangles[i].color, 1.0);
		}
	}
	for(int i = 0; i < spheres.length(); i++)
	{
		float t;
		if (SphereHit(spheres[i], ray, t) && t < distance)
		{
			distance = t;
			FragColor = vec4(spheres[i].color, 1.0);
		}
	}


}