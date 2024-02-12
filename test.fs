#version 330 core
in vec3 screenCoord;
out vec4 FragColor;

//screen space is ([0,1], [0,1], z in vertex shader)
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
	ray.direction = direction;

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

bool SphereHit(Sphere sphere, Ray ray)
{
	vec3 oc = ray.origin - sphere.center;
	
	float a = dot(ray.direction, ray.direction);
	float b = 2.0 * dot(oc, ray.direction);
	float c = dot(oc, oc) - sphere.radius * sphere.radius;

	float discriminant = b * b - 4 * a * c;

	return discriminant > 0.0;
}


void main()
{
	Ray ray;
	ray.origin = camPos;
	ray.direction = screenCoord - camPos;

	Sphere sphere;
	sphere.center = vec3(0.5,0.5,-1);
	sphere.radius = 0.5;

	Sphere sphereSmall;
	sphereSmall.center = vec3(0.2, 0.2, -1);
	sphereSmall.radius = 0.4;

	//test direction
	float dotCos = dot( normalize(ray.direction), vec3(0,0,-1));

	FragColor = vec4(dotCos,0, 0, 1.0);

	//background
	FragColor = vec4(0.1,0,0.2, 1.0);

	if(SphereHit(sphereSmall,ray)){
		FragColor = vec4(0.8,0, 0, 1.0);
	}
	if(SphereHit(sphere,ray)){
		FragColor = vec4(1,0, 0, 1.0);
	}
	

}