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

vec3 RayGetPointAt(Ray ray, float t)
{
	return ray.origin + t * ray.direction;
}

//sphere
struct Sphere 
{
    vec3 center;
    float radius;
}; 

Sphere SphereConstructor(vec3 center, float radius)
{
	Sphere sphere;
	sphere.center = center;
	sphere.radius = radius;
	return sphere;
}

// bool SphereHit(Sphere sphere, Ray ray)
// {
// 	vec3 oc = ray.origin - sphere.center;
	
// 	float a = dot(ray.direction, ray.direction);
// 	float b = 2.0 * dot(oc, ray.direction);
// 	float c = dot(oc, oc) - sphere.radius * sphere.radius;

// 	float discriminant = b * b - 4 * a * c;

// 	return discriminant > 0.0;
// }
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
};
Triangle TriangleConstructor(vec3[3] points)
{
	Triangle triangle;
	triangle.points = points;
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

	Sphere sphere0 = SphereConstructor(vec3(0.6, 0.6, -2), 0.7);
	Sphere sphere1 = SphereConstructor(vec3(-0.3, -0.15, -2.6), 0.6);
	Triangle triangle0 = TriangleConstructor(vec3[](vec3(-3, -1, -1.1), vec3(2, -1, -1.1), vec3(-3, -1, -8))); //left front, right front, left back
	Triangle triangle1 = TriangleConstructor(vec3[](vec3(2, -1, -1.1), vec3(2, -1, -8), vec3(-3, -1, -8))); //right front, right back, left back
	//test ray direction
	// float dotCos = dot( ray.direction , vec3(0,0,-1));
	// FragColor = vec4(dotCos,0, 0, 1.0);

	//background color
	FragColor = vec4(0.1,0,0.2, 1.0);

	//plane
	float t;
	if(InTriangle(triangle0, ray, t)){
		FragColor = vec4(0, 0.5, 0, 1.0);
	}
	if(InTriangle(triangle1, ray, t)){
		FragColor = vec4(0, 0.5, 0, 1.0);
	}
	
	if(SphereHit(sphere1,ray, t)){
		FragColor = vec4(0.8,0, 0, 1.0);
	}
	if(SphereHit(sphere0,ray, t)){
		FragColor = vec4(1,0, 0, 1.0);
	}
	

	
	

}