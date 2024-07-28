#version 330 core
in vec3 screenCoord;
out vec4 FragColor;

//======================= Ray Setup start =======================
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
//======================= Ray Setup end =======================

//======================= Light Setup start=======================
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
//======================= Light Setup end =======================

//======================= Triangle primitive setup and functions start =======================
struct Triangle
{
	vec3[3] points;
	float kd;
	vec3 color;
	vec3 altCheckerColor;
	float kr;
	float kt;
	float eta;
};
Triangle TriangleConstructor(vec3[3] points, float kd, vec3 color, vec3 altColor, float kr, float kt, float eta)
{
	Triangle triangle;
	triangle.points = points;
	triangle.kd = kd;
	triangle.color = color;
	triangle.altCheckerColor = altColor;
	triangle.kr = kr;
	triangle.kt = kt;
	triangle.eta = eta;
	return triangle;
}
bool InTriangle(Triangle triangle, Ray ray, out float tnear)
{
	//check if ray intersect with triangle, if it does, tnear is the distance of the ray
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
    }
	return isIn;
}

vec3 GetTriangleNormal(Triangle tri, Ray ray)
{
	//return normal for ray tracer
	vec3 normal = cross(tri.points[0] - tri.points[1], tri.points[1] - tri.points[2]);
	if(dot(normal, ray.direction) > 0) normal = -normal;
	return  normalize(normal);	
}
vec3 TriangleTextureColor(Triangle tri, vec3 intersectPoint)
{
	//return the checkered texture's color, 
	//checkpoint 4 start
	float checkerSize = 0.5;
	float leftEnd = 999.00, rightEnd = -999.00, nearEnd = -999.00, farEnd = 999.00;//z is negative
	for(int i = 0; i < 3; i++){
		if(tri.points[i].x < leftEnd) leftEnd = tri.points[i].x;
		if(tri.points[i].x > rightEnd) rightEnd = tri.points[i].x;
		if(tri.points[i].z > nearEnd) nearEnd = tri.points[i].z;
		if(tri.points[i].z < farEnd) farEnd = tri.points[i].z;
	}
	if (rightEnd < leftEnd || farEnd > nearEnd) return vec3(0,0,0);//error black color
	float width = rightEnd - leftEnd;
	float length = nearEnd - farEnd;

	int xChecker = int((intersectPoint.x - leftEnd) / checkerSize);
	int zChecker = int((nearEnd - intersectPoint.z ) / checkerSize);
	if ((xChecker - zChecker) % 2 == 0) return tri.color;
	else return tri.altCheckerColor;
	//checkpoint 4 end
}
//======================= Triangle primitive setup and functions end =======================

//======================= Sphere primitive setup and functions start =======================
struct Sphere 
{
    vec3 center;
    float radius;
	float kd;
	vec3 color;
	float kr;
	float kt;
	float eta;
}; 
Sphere SphereConstructor(vec3 center, float radius, float kd, vec3 color, float kr, float kt, float eta)
{
	Sphere sphere;
	sphere.center = center;
	sphere.radius = radius;
	sphere.kd = kd;
	sphere.color = color;
	sphere.kr = kr;
	sphere.kt = kt;
	sphere.eta = eta;
	return sphere;
}
bool SphereHit(Sphere sphere, Ray ray, out float tnear)
{
	//check if ray intersect with sphere, if it does, tnear is the distance of the ray
	vec3 L = sphere.center - ray.origin;
	float tca = dot(L, ray.direction);
	float d2 = dot(L, L) - tca * tca;
	if (d2 > sphere.radius * sphere.radius) return false;
	float thc = sqrt(sphere.radius * sphere.radius - d2);
	tnear = tca - thc;
	if (tnear < 0) return false;
	else return true;
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
//======================= Sphere primitive setup and functions end =======================

//======================= scene setup start =======================
//checkpoint 1 start
//screen space is (x: [0,1], y: [0,1], z = screenZ)
//all objects in screen space, zNear is -1 (object's position is no closer than -1)
vec3 camPos = vec3(0.5, 0.5, 0.0);

Sphere sphere1 = SphereConstructor(vec3(0.6, 0.6, -2), 0.7, 0.15,  vec3(0.8, 0.8, 0.8), 0.01, 0.8, 0.95);
Sphere sphere2 = SphereConstructor(vec3(-0.3, -0.15, -3), 0.6, 0.25, vec3(0.8, 0.8, 0.8), 0.75, 0.0, 0.95);
Sphere spheres[2] = Sphere[2](sphere1, sphere2);

Triangle triangle0 = TriangleConstructor(vec3[](vec3(-3, -1, 0), vec3(2, -1, 0), vec3(-3, -1, -8)), 0.6, vec3(1.0, 1.0, 0.1), vec3(1.0,0,0), 0, 0, 0); //left front, right front, left back
Triangle triangle1 = TriangleConstructor(vec3[](vec3(2, -1, 0), vec3(2, -1, -8), vec3(-3, -1, -8)), 0.6, vec3(1.0, 1.0, 0.1), vec3(1.0,0,0), 0, 0, 0); //right front, right back, left back
Triangle triangles[2] = Triangle[2](triangle0, triangle1);

Light light0 = LightConstructor(vec3(0, 4, 0), vec3(1,1,1), 1.0);
Light light1 = LightConstructor(vec3(-1, 1, -0.5), vec3(1,1,1), 1);
//Light lights[2] = Light[2](light0, light1); //For multiple light sources
Light lights[1] = Light[1](light0);//For submission, only one light in the scene
//checkpoint 1 end

vec3 backgroundColor = vec3(0.0, 0.6, 0.9);
vec3 ambientColor = vec3(1, 1, 1);
float specularExp = 20;
//======================= scene setup end =======================

//======================= encapsulation start =======================
float RayIntersect(Ray ray, out int objectIndex[2])
{
	//checkpoint 2 start
	//Calculate the ray's intersection with all objects in the scene
	//objectIndex[2] is how I keep track of the fitst object that this ray intersects with
	//objectIndex[-1][-1] means no object intersected
	//objectIndex[0][n] means the n th triangle
	//objectIndex[1][n] means the n th sphere
	objectIndex[0] = -1;
	objectIndex[1] = -1;
	float distance = 999999;
	for(int i = 0; i < triangles.length(); i++)
	{
		float t;
		if (InTriangle(triangles[i], ray, t) && t < distance)
		{
			distance = t;
			objectIndex = int[2](0, i);//object recorded
		}
	}
	for(int i = 0; i < spheres.length(); i++)
	{
		float t;
		if (SphereHit(spheres[i], ray, t) && t < distance)
		{
			distance = t;
			objectIndex = int[2](1, i);//object recorded
		}
	}
	return distance;
}

bool RayHitAnything(Ray ray)
{
	//a simpler ray-intersection function for shadow ray
	float tempDist;
	for(int i = 0; i < triangles.length(); i++)
	{
		if (InTriangle(triangles[i], ray, tempDist) && tempDist > 0 && triangles[i].kt < 0.001) return true;
	}
	for(int i = 0; i < spheres.length(); i++)
	{
		if (SphereHit(spheres[i], ray, tempDist) && tempDist > 0 && spheres[i].kt < 0.001) return true;
	}
	return false;
}
//checkpoint 2 end

//A bunch of functions to get fileds from specific object, using int[2] objectIndex to find the object
vec3 GetNormal(Ray ray, float distance, int[2] objectIndex){
	vec3 N = vec3(0,0,0);
	if(objectIndex[0] == 0) N = GetTriangleNormal(triangles[objectIndex[1]], ray);
	else if(objectIndex[0] == 1) N = GetSphereNormal(spheres[objectIndex[1]], ray.origin + ray.direction * distance);
	return N;	
}
float GetKd(int[2] objectIndex){
	if(objectIndex[0] == 0) return triangles[objectIndex[1]].kd;
	else if(objectIndex[0] == 1) return spheres[objectIndex[1]].kd;
}
float GetKr(int[2] objectIndex){
	if(objectIndex[0] == 0) return triangles[objectIndex[1]].kr;
	else if(objectIndex[0] == 1) return spheres[objectIndex[1]].kr;
}
float GetKt(int[2] objectIndex){
	if(objectIndex[0] == 0) return triangles[objectIndex[1]].kt;	
	else if(objectIndex[0] == 1) return spheres[objectIndex[1]].kt;
}
float GetEta(int[2] objectIndex){
	if(objectIndex[0] == 0) return triangles[objectIndex[1]].eta;
	else if(objectIndex[0] == 1) return spheres[objectIndex[1]].eta;
}

float ka = 0.15;
float ks = 0.4;

vec3 LightRayShading(Ray ray, float distance, Light light, int[2] objectIndex)
{
	//light ray (shadow ray, I think light ray makes more sense to me)
	//checkpoint 3 starts
	vec3 hitPoint = ray.origin + ray.direction * distance;
	vec3 lightRayDir = normalize(light.position - hitPoint);
	Ray lightRay = RayConstructor(hitPoint + lightRayDir * 0.01, lightRayDir);

	//if blocked by another object, return ambient color
	if (RayHitAnything(lightRay)){
		vec3 objectColor;
		if (objectIndex[0] == 0) objectColor = TriangleTextureColor(triangles[objectIndex[1]], ray.origin + ray.direction * distance);
		else objectColor = spheres[objectIndex[1]].color;

		return ka * (objectColor + ambientColor) / 2.0;
	}
	
	//if not blocked, do the phong-shading
	//get normal and object color
	vec3 N = GetNormal(ray, distance, objectIndex);
	vec3 objectColor;
	if(objectIndex[0] == 0) {
		objectColor = TriangleTextureColor(triangles[objectIndex[1]], ray.origin + ray.direction * distance);
	}
	else if(objectIndex[0] == 1) {
		objectColor = spheres[objectIndex[1]].color;
	}
	float kd = GetKd(objectIndex);	

	//blinn-phong shading
	float diffuseIntensity = light.intensity * max( 0.0, dot(lightRayDir, N) );
	float specularIntensity = light.intensity * pow(max(0.0, dot(Reflect(lightRayDir, N), ray.direction)), specularExp);
	return  ka * (objectColor + ambientColor) / 2.0 + kd * diffuseIntensity * objectColor + ks * specularIntensity * light.color;
	//checkpoint 3 ends
}

vec3 CastOneRay(Ray ray, out int objectIndex[2], out vec3 hitPos, out vec3 hitNormal)
{
	//shoots one ray from camera, and start the ray tracing
	//objectIndex[2]: triangle = 0, sphere = 1; if not indexing to any object, set to -1
	float distance = RayIntersect(ray, objectIndex);
	//if no intersect with object, return with background color
	if (objectIndex[0] < 0) return backgroundColor;
	 	
	hitPos = ray.origin + ray.direction * distance;
	hitNormal = GetNormal(ray, distance, objectIndex);

	//local shading
	vec3 colorSum = vec3(0,0,0);
	for(int i = 0; i < lights.length(); i++) colorSum += LightRayShading(ray, distance, lights[i], objectIndex);
		
	return colorSum;
}
//======================= encapsulation end =======================


//======================= Functions for reflection and refraction start =======================
//checkpoint 5 checkpoint 6 start
//since GLSL doesn't support recursion, I could only have several exactly the same functions with different names
//For grading, just look at ReflectAndRefract0, the others are the same
//recursion depth = 3
//main calls ReflectAndRefract0
//ReflectAndRefract0 calls ReflectAndRefract1
//ReflectAndRefract1 calls ReflectAndRefract2
const float eta_air = 1.0;
vec3 ReflectAndRefract2(Ray ray, float coefficient, int[2] objectIndex, vec3 hitPos, vec3 hitNormal){
	vec3 colorSum = vec3(0,0,0);

	colorSum += coefficient * CastOneRay(ray, objectIndex, hitPos, hitNormal);
	
	float kr = GetKr(objectIndex);
	if(kr > 0.001) {
		vec3 reflectionDir = normalize(ray.direction - 2.0 * hitNormal * dot(hitNormal, ray.direction));
		Ray rayReflect = RayConstructor(hitPos - ray.direction * 0.01, reflectionDir);
	}
	float kt = GetKt(objectIndex);
	if(kt > 0.001){
		float eta_it = 0.0;
		//inside or outside
		if(dot(ray.direction, hitNormal) < 0){ //ray from outside
			eta_it = eta_air/GetEta(objectIndex);
		}
		else { //ray from inside
			eta_it = GetEta(objectIndex)/eta_air;
			hitNormal = -hitNormal;
		}
		float dotProduct = -dot(ray.direction, hitNormal);
		float discriminant = 1 + (eta_it * eta_it * (dotProduct * dotProduct - 1));

		if(discriminant < 0){ //total internal reflection, use reflection

			vec3 reflectionDir = normalize(ray.direction - 2.0 * hitNormal * dot(hitNormal, ray.direction));
			Ray rayRefract = RayConstructor(hitPos - ray.direction * 0.01, reflectionDir);
		}
		else{//refraction
			vec3 refractionDir = normalize( eta_it * ray.direction + (eta_it * dotProduct - sqrt(discriminant)) * hitNormal );
			Ray rayRefract = RayConstructor(hitPos + ray.direction * 0.01, refractionDir);
		}			
	}
	
	return colorSum;
}
vec3 ReflectAndRefract1(Ray ray, float coefficient, int[2] objectIndex, vec3 hitPos, vec3 hitNormal){
	vec3 colorSum = vec3(0,0,0);

	colorSum += coefficient * CastOneRay(ray, objectIndex, hitPos, hitNormal);
	
	float kr = GetKr(objectIndex);
	if(kr > 0.001) {
		vec3 reflectionDir = normalize(ray.direction - 2.0 * hitNormal * dot(hitNormal, ray.direction));
		Ray rayReflect = RayConstructor(hitPos - ray.direction * 0.01, reflectionDir);
		colorSum += ReflectAndRefract2(rayReflect, coefficient * kr, objectIndex, hitPos, hitNormal);
	}
	float kt = GetKt(objectIndex);
	if(kt > 0.001){
		float eta_it = 0.0;
		//inside or outside
		if(dot(ray.direction, hitNormal) < 0){ //ray from outside
			eta_it = eta_air/GetEta(objectIndex);
		}
		else { //ray from inside
			eta_it = GetEta(objectIndex)/eta_air;
			hitNormal = -hitNormal;
		}
		float dotProduct = -dot(ray.direction, hitNormal);
		float discriminant = 1 + (eta_it * eta_it * (dotProduct * dotProduct - 1));

		if(discriminant < 0){ //total internal reflection, use reflection

			vec3 reflectionDir = normalize(ray.direction - 2.0 * hitNormal * dot(hitNormal, ray.direction));
			Ray rayRefract = RayConstructor(hitPos - ray.direction * 0.01, reflectionDir);
			colorSum += ReflectAndRefract2(rayRefract, coefficient * kt, objectIndex, hitPos, hitNormal);
		}
		else{//refraction
			vec3 refractionDir = normalize( eta_it * ray.direction + (eta_it * dotProduct - sqrt(discriminant)) * hitNormal );
			Ray rayRefract = RayConstructor(hitPos + ray.direction * 0.01, refractionDir);
			colorSum += ReflectAndRefract2(rayRefract, coefficient * kt, objectIndex, hitPos, hitNormal);
		}			
	}
	
	return colorSum;
}
vec3 ReflectAndRefract0(Ray ray, float coefficient, int[2] objectIndex, vec3 hitPos, vec3 hitNormal){
	vec3 colorSum = vec3(0,0,0);

	colorSum += coefficient * CastOneRay(ray, objectIndex, hitPos, hitNormal);
	
	//reflection
	float kr = GetKr(objectIndex);
	if(kr > 0.001) {
		vec3 reflectionDir = normalize(ray.direction - 2.0 * hitNormal * dot(hitNormal, ray.direction));
		Ray rayReflect = RayConstructor(hitPos - ray.direction * 0.01, reflectionDir);
		colorSum += ReflectAndRefract1(rayReflect, coefficient * kr, objectIndex, hitPos, hitNormal);
	}

	//refraction
	float kt = GetKt(objectIndex);
	if(kt > 0.001){
		float eta_it = 0.0;
		//inside or outside
		if(dot(ray.direction, hitNormal) < 0){ //ray from outside
			eta_it = eta_air/GetEta(objectIndex);
		}
		else { //ray from inside
			eta_it = GetEta(objectIndex)/eta_air;
			hitNormal = -hitNormal;
		}
		float dotProduct = -dot(ray.direction, hitNormal);
		float discriminant = 1 + (eta_it * eta_it * (dotProduct * dotProduct - 1));

		if(discriminant < 0){ //total internal reflection, use reflection
			vec3 reflectionDir = normalize(ray.direction - 2.0 * hitNormal * dot(hitNormal, ray.direction));
			Ray rayRefract = RayConstructor(hitPos - ray.direction * 0.01, reflectionDir);
			colorSum += ReflectAndRefract1(rayRefract, coefficient * kt, objectIndex, hitPos, hitNormal);
		}
		else{//refraction
			vec3 refractionDir = normalize( eta_it * ray.direction + (eta_it * dotProduct - sqrt(discriminant)) * hitNormal );
			Ray rayRefract = RayConstructor(hitPos + ray.direction * 0.01, refractionDir);
			colorSum += ReflectAndRefract1(rayRefract, coefficient * kt, objectIndex, hitPos, hitNormal);
		}			
	}
	
	return colorSum;
}
//checkpoint 5 checkpoint 6 end
//======================= Functions for reflection and refraction end =======================

//======================= Main =======================
void main()
{
	vec3 colorSum = vec3(0,0,0);

	//objectindex, hitPos and hitNormal are passed in and out
	int objectIndex[2] = int[2](-1, -1);
	vec3 hitPos = vec3(0,0,0);
	vec3 hitNormal = vec3(0,0,0);
	
	vec3 rayDir = normalize(screenCoord - camPos);
	Ray ray = RayConstructor(camPos, rayDir);

	colorSum += CastOneRay(ray, objectIndex, hitPos, hitNormal);//getting the color, objectIndex, hitPos and hitNormal

	//reflection
	float coefficient = 1.00;
	float kr = GetKr(objectIndex);
	if(kr > 0.001) {
		vec3 reflectionDir = normalize(ray.direction - 2.0 * hitNormal * dot(hitNormal, ray.direction));
		Ray rayReflect = RayConstructor(hitPos - ray.direction * 0.01, reflectionDir);
		colorSum += ReflectAndRefract0(rayReflect, coefficient * kr, objectIndex, hitPos, hitNormal);
	}

	//refraction
	float kt = GetKt(objectIndex);
	if(kt > 0.001){
		float eta_it = 0.0;
		//inside or outside
		if(dot(ray.direction, hitNormal) < 0){ //ray from outside
			eta_it = eta_air/GetEta(objectIndex);
		}
		else { //ray from inside
			eta_it = GetEta(objectIndex)/eta_air;
			hitNormal = -hitNormal;
		}
		float dotProduct = -dot(ray.direction, hitNormal);
		float discriminant = 1 + (eta_it * eta_it * (dotProduct * dotProduct - 1));

		if(discriminant < 0){ //total internal reflection, use reflection

			vec3 reflectionDir = normalize(ray.direction - 2.0 * hitNormal * dot(hitNormal, ray.direction));
			Ray rayRefract = RayConstructor(hitPos - ray.direction * 0.01, reflectionDir);
			colorSum += ReflectAndRefract0(rayRefract, coefficient * kt, objectIndex, hitPos, hitNormal);
		}
		else{//refraction
			vec3 refractionDir = normalize( eta_it * ray.direction + (eta_it * dotProduct - sqrt(discriminant)) * hitNormal );
			Ray rayRefract = RayConstructor(hitPos + ray.direction * 0.01, refractionDir);
			colorSum += ReflectAndRefract0(rayRefract, coefficient * kt, objectIndex, hitPos, hitNormal);
		}
	}	
	
	FragColor = vec4 (colorSum, 1.0);

	
}