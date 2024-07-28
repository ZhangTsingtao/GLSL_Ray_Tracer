//I'm doing ray tracing in fragment shader
//main.cpp setup the drawing canvas and the camera's Z value as modifier on FOV
//the ray tracer code is in RayTracer.fs
#include <glad/glad.h>
#include <GLFW/glfw3.h>
#include "CImg.h"
#include <iostream> 
#include <fstream>

#include "shader.h"


using namespace cimg_library;
using namespace std;

#define WindowSizeX 800
#define WindowSizeY 800
// settings
float screenZ = -0.5f;

void framebuffer_size_callback(GLFWwindow* window, int width, int height);
void processInput(GLFWwindow* window);

//preparations for saving and loading bmp file
//The code for checkpoint 7 tone reproduction starts from line 116
#pragma pack(2)

typedef unsigned char  BYTE;
typedef unsigned short WORD;
typedef unsigned long  DWORD;
typedef long    LONG;


//BMP head
struct MYBITMAPFILEHEADER
{
    WORD  bfType;      
    DWORD bfSize;      
    WORD  bfReserved1; 
    WORD  bfReserved2; 
    DWORD bfOffBits;   
};

//BMP
struct MYBITMAPINFOHEADER
{
    DWORD biSize;          
    LONG  biWidth;         
    LONG  biHeight;        
    WORD  biPlanes;  
    WORD  biBitCount;      
    DWORD biCompression;   
    DWORD biSizeImage;     
    LONG  biXPelsPerMeter; 
    LONG  biYPelsPerMeter; 
    DWORD biClrUsed;       
    DWORD biClrImportant;  
};


struct RGBColor
{
    char B;
    char G;
    char R;
};



void WriteBMP(const char* FileName, RGBColor* ColorBuffer, int ImageWidth, int ImageHeight)
{
    const int ColorBufferSize = ImageHeight * ImageWidth * sizeof(RGBColor);

    //file header
    MYBITMAPFILEHEADER fileHeader;
    fileHeader.bfType = 0x4D42;    //0x42ÊÇ'B'£»0x4DÊÇ'M'
    fileHeader.bfReserved1 = 0;
    fileHeader.bfReserved2 = 0;
    fileHeader.bfSize = sizeof(MYBITMAPFILEHEADER) + sizeof(MYBITMAPINFOHEADER) + ColorBufferSize;
    fileHeader.bfOffBits = sizeof(MYBITMAPFILEHEADER) + sizeof(MYBITMAPINFOHEADER);

    //data header
    MYBITMAPINFOHEADER bitmapHeader = { 0 };
    bitmapHeader.biSize = sizeof(MYBITMAPINFOHEADER);
    bitmapHeader.biHeight = ImageHeight;
    bitmapHeader.biWidth = ImageWidth;
    bitmapHeader.biPlanes = 1;
    bitmapHeader.biBitCount = 24;
    bitmapHeader.biSizeImage = ColorBufferSize;
    bitmapHeader.biCompression = 0; //BI_RGB


    FILE* fp;

    fopen_s(&fp, FileName, "wb");

    fwrite(&fileHeader, sizeof(MYBITMAPFILEHEADER), 1, fp);
    fwrite(&bitmapHeader, sizeof(MYBITMAPINFOHEADER), 1, fp);
    fwrite(ColorBuffer, ColorBufferSize, 1, fp);

    fclose(fp);

}

//take the last frame and store the image
void ScreenShot()
{
    RGBColor* ColorBuffer = new RGBColor[WindowSizeX * WindowSizeY];

    glReadPixels(0, 0, WindowSizeX, WindowSizeY, GL_BGR, GL_UNSIGNED_BYTE, ColorBuffer);//BMP store in BGR 

    WriteBMP("./tr_output/tr_base.bmp", ColorBuffer, WindowSizeX, WindowSizeY);

    delete[] ColorBuffer;

}

//checkpoint 7 start
const double Ldmax = 500;
//tone reproduction
void wardModel(CImg<unsigned char> Img, double Lmax) 
{
    //Ward's model
    double Lwa = 0;
    cimg_forXY(Img, x, y) {
        //step 1 and 2, get the overall luminance
        double L = Lmax * (0.27 * (double)Img(x, y, 0) / 255.00 + 0.67 * (double)Img(x, y, 1) / 255.00 + 0.06 * (double)Img(x, y, 2) / 255.00);

        //step 3, first, add up Lwa
        Lwa += log(0.00001 + L) / (double)(WindowSizeX * WindowSizeY);
    }
    Lwa = exp(Lwa);//get the Lwa
    double sf = pow((1.219 + pow(Ldmax / 2, 0.4)) / (1.219 + pow(Lwa, 0.4)), 2.5);//get sf

    //apply sf to RGB
    double LAve = 0;
    cimg_forXY(Img, x, y) {
        if (Lmax * sf * Img(x, y, 0) / Ldmax > 255) Img(x, y, 0) = 255;
        else Img(x, y, 0) = Lmax * sf * (double)Img(x, y, 0) / Ldmax;

        if (Lmax * sf * Img(x, y, 1) / Ldmax > 255) Img(x, y, 1) = 255;
        else Img(x, y, 1) = Lmax * sf * (double)Img(x, y, 1) / Ldmax;

        if (Lmax * sf * Img(x, y, 2) / Ldmax > 255) Img(x, y, 2) = 255;
        else Img(x, y, 2) = Lmax * sf * (double)Img(x, y, 2) / Ldmax;

        LAve += (double)(Img(x, y, 0) + Img(x, y, 1) + Img(x, y, 2)) / (3 * WindowSizeX * WindowSizeY);
    }

    Img.save_bmp("./tr_output/tr_ward_10nit.bmp");
}

void reinhardModel(CImg<unsigned char> Img, double Lmax) 
{
    //Reinhard's model
    double Lwa = 0;
    cimg_forXY(Img, x, y) {
        //step 1 and 2, get the overall luminance
        double L = Lmax * (0.27 * (double)Img(x, y, 0) / 255.00 + 0.67 * (double)Img(x, y, 1) / 255.00 + 0.06 * (double)Img(x, y, 2) / 255.00);

        //step 3, first, add up Lwa
        Lwa += log(0.00001 + L) / (double)(WindowSizeX * WindowSizeY);
    }
    Lwa = exp(Lwa);//get the Lwa

    double a = 0.36;
    cimg_forXY(Img, x, y) {
        double Rscaled = Lmax * a * (double)Img(x, y, 0) / (Lwa * 255);
        double Gscaled = Lmax * a * (double)Img(x, y, 1) / (Lwa * 255);
        double Bscaled = Lmax * a * (double)Img(x, y, 2) / (Lwa * 255);

        if (255 * Rscaled / (1 + Rscaled) > 255) Img(x, y, 0) = 255;
        else Img(x, y, 0) = 255 * Rscaled / (1 + Rscaled);
        if (255 * Gscaled / (1 + Gscaled) > 255) Img(x, y, 1) = 255;
        else Img(x, y, 1) = 255 * Gscaled / (1 + Gscaled);
        if (255 * Bscaled / (1 + Bscaled) > 255) Img(x, y, 2) = 255;
        else Img(x, y, 2) = 255 * Bscaled / (1 + Bscaled);
    }

    Img.save_bmp("./tr_output/tr_reinhard_100nit_0.36.bmp");
}
//advanced 4 start
void ALMmodel(CImg<unsigned char> Img, double Lmax) {
    //Advanced: Adaptive Logarithmic Mapping
    double Lwa = 0;
    cimg_forXY(Img, x, y) {
        double L = Lmax * (0.27 * (double)Img(x, y, 0) / 255.00 + 0.67 * (double)Img(x, y, 1) / 255.00 + 0.06 * (double)Img(x, y, 2) / 255.00);

        Lwa += log(0.00001 + L) / (double)(WindowSizeX * WindowSizeY);
    }
    Lwa = exp(Lwa);
    cout << "Lwa: " << Lwa << endl;

    double Lwmax = Lmax / Lwa;

    double b = 0.85;
    cimg_forXY(Img, x, y) {
        double L = Lmax * (0.27 * (double)Img(x, y, 0) / 255.00 + 0.67 * (double)Img(x, y, 1) / 255.00 + 0.06 * (double)Img(x, y, 2) / 255.00);
        double Lwmax = Lmax / Lwa;
        double Lw = L / Lwa;
        double Ld = 1.000 / log10(Lwmax + 1) * log(Lw + 1) / log(2 + pow(Lw / Lwmax, log(b) / log(0.5)) * 8);

        Img(x, y, 0) *= Ld;
        Img(x, y, 1) *= Ld;
        Img(x, y, 2) *= Ld;
    }
    Img.save_bmp("./tr_output/tr_ALM_10nit.bmp");
}
//advanced 4 end

void toneReproduction()
{
    CImg<unsigned char> SrcImg;
    SrcImg.load_bmp("./tr_output/tr_base.bmp");

    CImg<unsigned char> Img = SrcImg; //0,1,2 => R G B

    double Lmax = 100;

    //To apply tone reproduction, uncomment any of the three functions below and see the result in ./tr_output
    //wardModel(Img, Lmax);//checkpoint 7
    //reinhardModel(Img, Lmax);//checkpoint 7
    //ALMmodel(Img, Lmax);//advanced 4
}
//checkpoint 7 end

int main()
{
    // glfw: initialize and configure
    // ------------------------------
    glfwInit();
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
    glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);

#ifdef __APPLE__
    glfwWindowHint(GLFW_OPENGL_FORWARD_COMPAT, GL_TRUE);
#endif

    // glfw window creation
    // --------------------
    GLFWwindow* window = glfwCreateWindow(WindowSizeX, WindowSizeY, "Ray Tracer", NULL, NULL);
    if (window == NULL)
    {
        std::cout << "Failed to create GLFW window" << std::endl;
        glfwTerminate();
        return -1;
    }
    glfwMakeContextCurrent(window);
    glfwSetFramebufferSizeCallback(window, framebuffer_size_callback);

    // glad: load all OpenGL function pointers
    // ---------------------------------------
    if (!gladLoadGLLoader((GLADloadproc)glfwGetProcAddress))
    {
        std::cout << "Failed to initialize GLAD" << std::endl;
        return -1;
    }

    // build and compile our shader program
    // ------------------------------------
    Shader ourShader("RayTracer.vs", "RayTracer.fs");
    // set up vertex data (and buffer(s)) and configure vertex attributes
    // ------------------------------------------------------------------
    float vertices[] = {
         1.0f,  1.0f, 0.0f,  // top right
         1.0f, -1.0f, 0.0f,  // bottom right
        -1.0f, -1.0f, 0.0f,  // bottom left
        -1.0f,  1.0f, 0.0f   // top left 
    };
    unsigned int indices[] = {  // note that we start from 0!
        0, 1, 3,  // first Triangle
        1, 2, 3   // second Triangle
    };
    unsigned int VBO, VAO, EBO;
    glGenVertexArrays(1, &VAO);
    glGenBuffers(1, &VBO);
    glGenBuffers(1, &EBO);
    // bind the Vertex Array Object first, then bind and set vertex buffer(s), and then configure vertex attributes(s).
    glBindVertexArray(VAO);

    glBindBuffer(GL_ARRAY_BUFFER, VBO);
    glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);

    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, EBO);
    glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(indices), indices, GL_STATIC_DRAW);

    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);

    // note that this is allowed, the call to glVertexAttribPointer registered VBO as the vertex attribute's bound vertex buffer object so afterwards we can safely unbind
    glBindBuffer(GL_ARRAY_BUFFER, 0);

    // remember: do NOT unbind the EBO while a VAO is active as the bound element buffer object IS stored in the VAO; keep the EBO bound.
    //glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);

    // You can unbind the VAO afterwards so other VAO calls won't accidentally modify this VAO, but this rarely happens. Modifying other
    // VAOs requires a call to glBindVertexArray anyways so we generally don't unbind VAOs (nor VBOs) when it's not directly necessary.
    glBindVertexArray(0);

    // uncomment this call to draw in wireframe polygons.
    //glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);



    // render loop
    // -----------
    while (!glfwWindowShouldClose(window)) //keep updating
    {
        processInput(window);

        // render
        glClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        glClear(GL_COLOR_BUFFER_BIT);

        // render the triangle as canvas
        ourShader.use();
        glBindVertexArray(VAO); // seeing as we only have a single VAO there's no need to bind it every time, but we'll do so to keep things a bit more organized
        //glDrawArrays(GL_TRIANGLES, 0, 6);

        ourShader.setFloat("screenZ", screenZ);

        glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0);
        // glBindVertexArray(0); // no need to unbind it every time 

        // glfw: swap buffers and poll IO events (keys pressed/released, mouse moved etc.)
        glfwSwapBuffers(window);
        glfwPollEvents(); 
    }
    ScreenShot();
    toneReproduction();

    // optional: de-allocate all resources once they've outlived their purpose:
    // ------------------------------------------------------------------------
    glDeleteVertexArrays(1, &VAO);
    glDeleteBuffers(1, &VBO);

    // glfw: terminate, clearing all previously allocated GLFW resources.
    // ------------------------------------------------------------------
    glfwTerminate();
    return 0;
}

// process all input: query GLFW whether relevant keys are pressed/released this frame and react accordingly
// ---------------------------------------------------------------------------------------------------------
void processInput(GLFWwindow* window)
{
    if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
        glfwSetWindowShouldClose(window, true);
    if (glfwGetKey(window, GLFW_KEY_W) == GLFW_PRESS) screenZ -= 0.001f;   
    if (glfwGetKey(window, GLFW_KEY_S) == GLFW_PRESS) screenZ += 0.001f;
}

// glfw: whenever the window size changed (by OS or user resize) this callback function executes
// ---------------------------------------------------------------------------------------------
void framebuffer_size_callback(GLFWwindow* window, int width, int height)
{
    // make sure the viewport matches the new window dimensions; note that width and 
    // height will be significantly larger than specified on retina displays.
    glViewport(0, 0, width, height);
}