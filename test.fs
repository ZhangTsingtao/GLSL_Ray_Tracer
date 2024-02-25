#version 330 core
in vec3 screenCoord;
out vec4 FragColor;

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
struct Light{
	vec3 position;
	vec3 color;
	float intensity;
};
Light LightConstructor(vec3 position, vec3 color, float intensity)
{
	Light light;
	light.position = position;
	light.color = color;
	light.intensity = intensity;
	return light;
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
vec3 GetTriangleNormal(Triangle tri, Ray ray)
{
	vec3 normal = cross(tri.points[0] - tri.points[1], tri.points[1] - tri.points[2]);
	if(dot(normal, ray.direction) > 0) normal = -normal;
	return  normalize(normal);
}

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
vec3 GetSphereNormal(Sphere sphere, vec3 hitPosition)
{
	return normalize(hitPosition - sphere.center);
}

vec3 Reflect(vec3 I, vec3 N)
{
	vec3 i = normalize(I);
	vec3 n = normalize(N);
	return i - 2.0 * dot(i, n) * n;
}

Ray ReflectRay(Ray inputRay, vec3 N, vec3 reflectPoint)
{
	vec3 i = normalize(inputRay.direction);
	vec3 n = normalize(N);
	return RayConstructor(reflectPoint, i - 2.0 * dot(i, n) * n);
}

vec3 BlinnPhongModel(vec3 objectColor, vec3 ambientColor, Light light, vec3 inputRayDir, vec3 lightRayDir, vec3 N, float specularExp)
{
	float diffuseIntensity = light.intensity * max( 0.0, dot(lightRayDir, N) );
	float specularIntensity = light.intensity * pow(max(0.0, dot(Reflect(lightRayDir, N), inputRayDir)), specularExp);
	return  0.7 * ambientColor + 0.5 * diffuseIntensity * objectColor + 0.4 * specularIntensity * light.color;
}

//scene setup
//screen space is ([0,1], [0,1], z in vertex shader)
//all objects in screen space, zNear is -1
vec3 camPos = vec3(0.5, 0.5, 0.0);

Sphere sphere1 = SphereConstructor(vec3(0.6, 0.6, -2), 0.7, vec3(0.8, 0.1, 0.1));
Sphere sphere2 = SphereConstructor(vec3(-0.3, -0.15, -2.6), 0.6, vec3(0.7, 0.2, 0.1));
Sphere spheres[2] = Sphere[2](sphere1, sphere2);

Triangle triangle0 = TriangleConstructor(vec3[](vec3(-3, -1, -1.1), vec3(2, -1, -1.1), vec3(-3, -1, -8)), vec3(0.4, 0.5, 0.1)); //left front, right front, left back
Triangle triangle1 = TriangleConstructor(vec3[](vec3(2, -1, -1.1), vec3(2, -1, -8), vec3(-3, -1, -8)), vec3(0.4, 0.5, 0.1)); //right front, right back, left back
Triangle triangles[2] = Triangle[2](triangle0, triangle1);

Light light = LightConstructor(vec3(1, 4, 0), vec3(1,1,1), 2);

vec3 backgroundColor = vec3(0.1, 0.1, 0.1);
vec3 ambientColor = vec3(0.2, 0.1, 0.1);
float specularExp = 100;

//modulate
float RayIntersect(Ray ray, out int objectIndex[2])
{
	//
	objectIndex[0] = -1;
	objectIndex[1] = -1;
	float distance = 999999;
	for(int i = 0; i < triangles.length(); i++)
	{
		float t;
		if (InTriangle(triangles[i], ray, t) && t < distance)
		{
			distance = t;
			//FragColor = vec4(triangles[i].color, 1.0);
			objectIndex = int[2](0, i);//object recorded
		}
	}
	for(int i = 0; i < spheres.length(); i++)
	{
		float t;
		if (SphereHit(spheres[i], ray, t) && t < distance)
		{
			distance = t;
			//FragColor = vec4(spheres[i].color, 1.0);
			objectIndex = int[2](1, i);//object recorded
		}
	}
	return distance;
}

bool RayHitAnything(Ray ray)
{
	float tempDist;
	for(int i = 0; i < triangles.length(); i++)
	{
		if (InTriangle(triangles[i], ray, tempDist) && tempDist > 0) return true;
	}
	for(int i = 0; i < spheres.length(); i++)
	{
		if (SphereHit(spheres[i], ray, tempDist) && tempDist > 0) return true;
	}
	return false;
}

void CastOneRay(vec3 screenPosition)
{
	Ray ray = RayConstructor(camPos, screenPosition - camPos);
	int objectIndex[2] = int[2](-1, -1);//triangle = 0, sphere = 1; if not indexing to any object, set to -1
	float distance = RayIntersect(ray, objectIndex);//ray intersection
	//if no intersect with object, return with background color
	if (objectIndex[0] < 0) {
		FragColor = vec4(backgroundColor, 1.0);
		return; 
	}
	
	//light ray
	vec3 hitPoint = ray.origin + ray.direction * distance;
	vec3 lightRayDir = normalize(light.position - hitPoint);
	Ray lightRay = RayConstructor(hitPoint + lightRayDir * 0.01, lightRayDir);

	//if blocked by another object, return ambient color
	if (RayHitAnything(lightRay)){
		FragColor = 0.7 * vec4(ambientColor, 1.0);
		return;
	}

	//get normal and object color
	vec3 N = vec3(0,0,0);
	vec3 objectColor;
	if(objectIndex[0] == 0) {
		N = GetTriangleNormal(triangles[objectIndex[1]], ray);
		objectColor = triangles[objectIndex[1]].color;
	}
	else if(objectIndex[0] == 1) {
		N = GetSphereNormal(spheres[objectIndex[1]], ray.origin + ray.direction * distance);
		objectColor = spheres[objectIndex[1]].color;
	}	

	//blinn-phong shading
	vec3 phongColor = BlinnPhongModel(objectColor, ambientColor, light, ray.direction, lightRay.direction, N, specularExp);
	FragColor = vec4(phongColor, 1.0);
}
void main()
{
	CastOneRay(screenCoord);
	
}