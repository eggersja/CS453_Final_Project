#include <stdlib.h>
#include <stdio.h>
#include <math.h>
#include <string.h>
#include <fstream>
#include <vector>
#include <iostream>
#include <string>

#include "glError.h"
#include "gl/glew.h"
#include "gl/freeglut.h"
#include "ply.h"
#include "icVector.H"
#include "icMatrix.H"
#include "polyhedron.h"
#include "polyline.h"
#include "trackball.h"
#include "tmatrix.h"

using std::cout;
using std::cin;
using std::endl;
using std::vector;
using std::string;

Polyhedron* poly;

/* random globals */
vector<PolyLine> streamlines;
vector<LineSegment> vectors;
bool displayStreamlines = false;

/*scene related variables*/
const float zoomspeed = 0.9;
const int view_mode = 0;		// 0 = othogonal, 1=perspective
const double radius_factor = 1.0;
int win_width = 800;
int win_height = 800;
float aspectRatio = win_width / win_height;

bool scene_lights_on = true;

/*file management related variables*/
/// <remarks>Must equal the number of items in the array declared below.</remarks>
const int LOADABLE_COUNT = 8;

/// <summary>
/// An array containing all of the paths to the files to load. Can be iterated through with <see cref="keyboard(unsigned char key, int x, int y)">'x'</see>
/// </summary>
/// <remarks>
/// "../quadmesh_2D/fun_shapes/face.ply" for dummy
/// </remarks>
const char* LOAD_PATHS[LOADABLE_COUNT] = {
	"../datasets/proc_boids_basic/basic.t1.boids.ply", // 0
	"../datasets/proc_boids_basic/basic.t2.boids.ply", // 1
	"../datasets/proc_boids_basic/basic.t3.boids.ply", // 2
	"../datasets/proc_boids_basic/basic.t4.boids.ply", // 3
	"../datasets/proc_boids_basic/basic.t5.boids.ply", // 4
	"../datasets/proc_boids_basic/basic.t6.boids.ply", // 5
	"../datasets/proc_boids_basic/basic.t7.boids.ply", // 6
	"../datasets/proc_boids_basic/basic.t8.boids.ply"  // 7
};

/// <summary>
/// Determines which scalar load path to use. Acceptable values are 0-7
/// </summary>
int load_selector = 0;

/*
Use keys 1 to 0 to switch among different display modes.
Each display mode can be designed to show one type 
visualization result.

Predefined ones: 
display mode 1: solid rendering
display mode 2: show wireframes
display mode 3: render each quad with colors of vertices
display mode 4: Drawing example
display mode 5: Image-based Flow Visualization (IBFV)
display mode 6: Grayscale scalar field.
*/
int display_mode = 1;

/* changing file variables */
int current_t = 1;
#define MAX_T 11
#define MIN_T 1

/*User Interaction related variabes*/
float s_old, t_old;
float rotmat[4][4];
double zoom = 1.0;
double translation[2] = { 0, 0 };
int mouse_mode = -2;	// -1 = no action, 1 = tranlate y, 2 = rotate

/*IBFV related variables*/
//https://www.win.tue.nl/~vanwijk/ibfv/
#define	NPN 64
#define SCALE 4.0
int    Npat = 32;
int    iframe = 0;
float  tmax = win_width / (SCALE*NPN);
float  dmax = SCALE / win_width;
unsigned char *pixels;

#define DM  ((float) (1.0/(100-1.0)))

/******************************************************************************
Forward declaration of functions
******************************************************************************/

void init(void);
void makePatterns(void);

/* custom functions */
void extract_streamline(double, double, double, PolyLine&);
icVector3 getDir(double, double, double);
void gatherStreamlines();
void gatherVectors();

/*glut attaching functions*/
void keyboard(unsigned char key, int x, int y);
void motion(int x, int y);
void display(void);
void mouse(int button, int state, int x, int y);
void mousewheel(int wheel, int direction, int x, int y);
void reshape(int width, int height);

/*functions for element picking*/
void display_vertices(GLenum mode, Polyhedron* poly);
void display_quads(GLenum mode, Polyhedron* poly);
void display_selected_vertex(Polyhedron* poly);
void display_selected_quad(Polyhedron* poly);

/*display vis results*/
void display_polyhedron(Polyhedron* poly);

/*display utilities*/

void scalar_bounds(Polyhedron* poly, double* lower, double* upper);

void display_grayscale_quad(Quad* qu, double lower, double upper);

void display_bicolor_quad(Quad* qu, double lower, double upper, float lower_color[3], float upper_color[3]);

void display_heightmod_quad(Quad* qu, double lower, double upper, float ref_color[3], float peak);

void display_grayscale_heightmod_quad(Quad* qu, double lower, double upper, float peak);

void display_bicolor_heightmod_quad(Quad* qu, double lower, double upper, float lower_color[3], float upper_color[3], float peak);

/*file management*/
/// <summary>
/// Loads a polyhedron from a file and outputs to the <see cref="poly"/> global variable.
/// </summary>
/// <param name="ply_path">The path to the polyhedron to be loaded. Must be in PLY format</param>
void load_ply(char* ply_path);


/*
draw a sphere
x, y, z are the coordiate of the dot
radius of the sphere 
R: the red channel of the color, ranges [0, 1]
G: the green channel of the color, ranges [0, 1]
B: the blue channel of the color, ranges [0, 1]
*/
void drawDot(double x, double y, double z, double radius = 0.1, double R = 1.0, double G = 0.0, double B = 0.0) {

	glEnable(GL_POLYGON_OFFSET_FILL);
	glPolygonOffset(1., 1.);
	glEnable(GL_DEPTH_TEST);
	glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
	glShadeModel(GL_SMOOTH);
	glEnable(GL_LIGHTING);
	glEnable(GL_LIGHT0);
	glEnable(GL_LIGHT1);

	GLfloat mat_diffuse[4];

	{
		mat_diffuse[0] = R;
		mat_diffuse[1] = G;
		mat_diffuse[2] = B;
		mat_diffuse[3] = 1.0;
	}

	glMaterialfv(GL_FRONT, GL_DIFFUSE, mat_diffuse);

	GLUquadric* quad = gluNewQuadric();

	glPushMatrix();
	glTranslatef(x, y, z);
	gluSphere(quad, radius, 50, 50);
	glPopMatrix();

	gluDeleteQuadric(quad);
}

/*
draw a line segment
width: the width of the line, should bigger than 0
R: the red channel of the color, ranges [0, 1]
G: the green channel of the color, ranges [0, 1]
B: the blue channel of the color, ranges [0, 1]
*/
void drawLineSegment(LineSegment ls, double width = 1.0, double R = 1.0, double G = 0.0, double B = 0.0) {

	glDisable(GL_LIGHTING);
	glEnable(GL_LINE_SMOOTH);
	glHint(GL_LINE_SMOOTH_HINT, GL_NICEST);
	glEnable(GL_BLEND);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	glLineWidth(width);

	glBegin(GL_LINES);
	glColor3f(R, G, B);
	glVertex3f(ls.start.x, ls.start.y, ls.start.z);
	glVertex3f(ls.end.x, ls.end.y, ls.end.z);
	glEnd();

	glDisable(GL_BLEND);
}

/*
draw a polyline
width: the width of the line, should bigger than 0
R: the red channel of the color, ranges [0, 1]
G: the green channel of the color, ranges [0, 1]
B: the blue channel of the color, ranges [0, 1]
*/
void drawPolyline(PolyLine pl, double width = 1.0, double R = 1.0, double G = 0.0, double B = 0.0) {
	
	glDisable(GL_LIGHTING);
	glEnable(GL_LINE_SMOOTH);
	glHint(GL_LINE_SMOOTH_HINT, GL_NICEST);
	glEnable(GL_BLEND);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	glLineWidth(width);

	glBegin(GL_LINES);
	glColor3f(R, G, B);

	for (int i = 0; i < pl.size(); i++) {
		glVertex3f(pl[i].start.x, pl[i].start.y, pl[i].start.z);
		glVertex3f(pl[i].end.x, pl[i].end.y, pl[i].end.z);
	}

	glEnd();

	glDisable(GL_BLEND);
}

/******************************************************************************
Main program.
******************************************************************************/
int main(int argc, char* argv[])
{
	/*load mesh from ply file*/
	//Original path: "../quadmesh_2D/fun_shapes/face.ply"
	char* to_load = new char[256];
	strcpy(to_load, LOAD_PATHS[load_selector]);
	load_ply(to_load);
	
	/*initialize the mesh*/
	poly->initialize(); // initialize the mesh
	// poly->write_info();


	/*init glut and create window*/
	glutInit(&argc, argv);
	glutInitDisplayMode(GLUT_DOUBLE | GLUT_RGB);
	glutInitWindowPosition(20, 20);
	glutInitWindowSize(win_width, win_height);
	glutCreateWindow("Scientific Visualization");


	/*initialize openGL*/
	init();

	/*prepare the noise texture for IBFV*/
	makePatterns();
	
	/*the render function and callback registration*/
	glutKeyboardFunc(keyboard);
	glutReshapeFunc(reshape);
	glutDisplayFunc(display);
	glutIdleFunc(display);
	glutMotionFunc(motion);
	glutMouseFunc(mouse);
	glutMouseWheelFunc(mousewheel);
	
	/*event processing loop*/
	glutMainLoop();
	
	/*clear memory before exit*/
	poly->finalize();	// finalize everything
	free(pixels);
	return 0;
}


/******************************************************************************
Set projection mode
******************************************************************************/

void set_view(GLenum mode)
{
	GLfloat light_ambient0[] = { 0.3, 0.3, 0.3, 1.0 };
	GLfloat light_diffuse0[] = { 0.7, 0.7, 0.7, 1.0 };
	GLfloat light_specular0[] = { 0.0, 0.0, 0.0, 1.0 };

	GLfloat light_ambient1[] = { 0.0, 0.0, 0.0, 1.0 };
	GLfloat light_diffuse1[] = { 0.5, 0.5, 0.5, 1.0 };
	GLfloat light_specular1[] = { 0.0, 0.0, 0.0, 1.0 };

	glLightfv(GL_LIGHT0, GL_AMBIENT, light_ambient0);
	glLightfv(GL_LIGHT0, GL_DIFFUSE, light_diffuse0);
	glLightfv(GL_LIGHT0, GL_SPECULAR, light_specular0);

	glLightfv(GL_LIGHT1, GL_AMBIENT, light_ambient1);
	glLightfv(GL_LIGHT1, GL_DIFFUSE, light_diffuse1);
	glLightfv(GL_LIGHT1, GL_SPECULAR, light_specular1);


	glMatrixMode(GL_PROJECTION);
	if (mode == GL_RENDER)
		glLoadIdentity();

	if (aspectRatio >= 1.0) {
		if (view_mode == 0)
			glOrtho(-radius_factor * zoom * aspectRatio, radius_factor * zoom * aspectRatio, -radius_factor * zoom, radius_factor * zoom, -1000, 1000);
		else
			glFrustum(-radius_factor * zoom * aspectRatio, radius_factor * zoom * aspectRatio, -radius_factor* zoom, radius_factor* zoom, 0.1, 1000);
	}
	else {
		if (view_mode == 0)
			glOrtho(-radius_factor * zoom, radius_factor * zoom, -radius_factor * zoom / aspectRatio, radius_factor * zoom / aspectRatio, -1000, 1000);
		else
			glFrustum(-radius_factor * zoom, radius_factor * zoom, -radius_factor* zoom / aspectRatio, radius_factor* zoom / aspectRatio, 0.1, 1000);
	}


	GLfloat light_position[3];
	glMatrixMode(GL_MODELVIEW);
	glLoadIdentity();
	light_position[0] = 5.5;
	light_position[1] = 0.0;
	light_position[2] = 0.0;
	glLightfv(GL_LIGHT0, GL_POSITION, light_position);
	light_position[0] = -0.1;
	light_position[1] = 0.0;
	light_position[2] = 0.0;
	glLightfv(GL_LIGHT2, GL_POSITION, light_position);
}

/******************************************************************************
Update the scene
******************************************************************************/

void set_scene(GLenum mode, Polyhedron* poly)
{
	glTranslatef(translation[0], translation[1], -3.0);

	/*multiply rotmat to current mat*/
	{
		int i, j, index = 0;

		GLfloat mat[16];

		for (i = 0; i < 4; i++)
			for (j = 0; j < 4; j++)
				mat[index++] = rotmat[i][j];

		glMultMatrixf(mat);
	}

	glScalef(0.9 / poly->radius, 0.9 / poly->radius, 0.9 / poly->radius);
	glTranslatef(-poly->center.entry[0], -poly->center.entry[1], -poly->center.entry[2]);
}


/******************************************************************************
Init scene
******************************************************************************/

void init(void) {

	mat_ident(rotmat);

	/* select clearing color */
	glClearColor(0.0, 0.0, 0.0, 0.0);  // background
	glShadeModel(GL_FLAT);
	glPolygonMode(GL_FRONT, GL_FILL);

	glDisable(GL_DITHER);
	glEnable(GL_DEPTH_TEST);
	glDepthFunc(GL_LESS);
	
	//set pixel storage modes
	glPixelStorei(GL_PACK_ALIGNMENT, 1);
	
	glEnable(GL_NORMALIZE);
	if (poly->orientation == 0)
		glFrontFace(GL_CW);
	else
		glFrontFace(GL_CCW);
}


/******************************************************************************
Pick objects from the scene
******************************************************************************/

int processHits(GLint hits, GLuint buffer[])
{
	unsigned int i, j;
	GLuint names, * ptr;
	double smallest_depth = 1.0e+20, current_depth;
	int seed_id = -1;
	unsigned char need_to_update;

	ptr = (GLuint*)buffer;
	for (i = 0; i < hits; i++) {  /* for each hit  */
		need_to_update = 0;
		names = *ptr;
		ptr++;

		current_depth = (double)*ptr / 0x7fffffff;
		if (current_depth < smallest_depth) {
			smallest_depth = current_depth;
			need_to_update = 1;
		}
		ptr++;
		current_depth = (double)*ptr / 0x7fffffff;
		if (current_depth < smallest_depth) {
			smallest_depth = current_depth;
			need_to_update = 1;
		}
		ptr++;
		for (j = 0; j < names; j++) {  /* for each name */
			if (need_to_update == 1)
				seed_id = *ptr - 1;
			ptr++;
		}
	}
	return seed_id;
}

/******************************************************************************
Diaplay all quads for selection
******************************************************************************/

void display_quads(GLenum mode, Polyhedron* this_poly)
{
	unsigned int i, j;
	GLfloat mat_diffuse[4];

	glEnable(GL_POLYGON_OFFSET_FILL);
	glPolygonOffset(1., 1.);
	glEnable(GL_DEPTH_TEST);
	glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
	glShadeModel(GL_SMOOTH);
	//glDisable(GL_LIGHTING);

	glEnable(GL_LIGHTING);
	glEnable(GL_LIGHT0);
	glEnable(GL_LIGHT1);
	glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);

	for (i = 0; i < this_poly->nquads; i++) {
		if (mode == GL_SELECT)
			glLoadName(i + 1);

		Quad* temp_q = this_poly->qlist[i];
		{
			mat_diffuse[0] = 1.0;
			mat_diffuse[1] = 1.0;
			mat_diffuse[2] = 0.0;
			mat_diffuse[3] = 1.0;
		}
		glMaterialfv(GL_FRONT, GL_DIFFUSE, mat_diffuse);
		
		glBegin(GL_POLYGON);
		for (j = 0; j < 4; j++) {
			Vertex* temp_v = temp_q->verts[j];
			//glColor3f(0, 0, 0);
			glVertex3d(temp_v->x, temp_v->y, temp_v->z);
		}
		glEnd();
	}
}

/******************************************************************************
Diaplay all vertices for selection
******************************************************************************/

void display_vertices(GLenum mode, Polyhedron* this_poly)
{
	glEnable(GL_POLYGON_OFFSET_FILL);
	glPolygonOffset(1., 1.);
	glEnable(GL_DEPTH_TEST);
	glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
	glShadeModel(GL_SMOOTH);
	glDisable(GL_LIGHTING);
	glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);

	for (int i = 0; i < this_poly->nverts; i++) {
		if (mode == GL_SELECT)
			glLoadName(i + 1);

		Vertex* temp_v = this_poly->vlist[i];

		{
			GLUquadric* quad = gluNewQuadric();

			glPushMatrix();
			glTranslatef(temp_v->x, temp_v->y, temp_v->z);
			glColor4f(0, 0, 1, 1.0);
			gluSphere(quad, this_poly->radius * 0.01, 50, 50);
			glPopMatrix();

			gluDeleteQuadric(quad);
		}
	}
}

/******************************************************************************
Diaplay selected quad
******************************************************************************/

void display_selected_quad(Polyhedron* this_poly)
{
	if (this_poly->selected_quad == -1)
	{
		return;
	}

	unsigned int i, j;
	GLfloat mat_diffuse[4];

	glEnable(GL_POLYGON_OFFSET_FILL);
	glPolygonOffset(1., 1.);
	glDisable(GL_DEPTH_TEST);
	glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
	glShadeModel(GL_SMOOTH);
	glDisable(GL_LIGHTING);

	Quad* temp_q = this_poly->qlist[this_poly->selected_quad];

	glBegin(GL_POLYGON);
	for (j = 0; j < 4; j++) {
		Vertex* temp_v = temp_q->verts[j];
		glColor3f(1.0, 0.0, 1.0);
		glVertex3d(temp_v->x, temp_v->y, 0.0);
	}
	glEnd();
}

/******************************************************************************
Diaplay selected vertex
******************************************************************************/

void display_selected_vertex(Polyhedron* this_poly)
{
	if (this_poly->selected_vertex == -1)
	{
		return;
	}

	Vertex* temp_v = this_poly->vlist[this_poly->selected_vertex];

	drawDot(temp_v->x, temp_v->y, temp_v->z, this_poly->radius * 0.01, 1.0, 0.0, 0.0);

}


/******************************************************************************
Callback function for glut window reshaped
******************************************************************************/

void reshape(int width, int height) {

	win_width = width;
	win_height = height;

	aspectRatio = (float)width / (float)height;

	glViewport(0, 0, width, height);

	set_view(GL_RENDER);

	/*Update pixels buffer*/
	free(pixels);
	pixels = (unsigned char *)malloc(sizeof(unsigned char)*win_width*win_height * 3);
	memset(pixels, 255, sizeof(unsigned char)*win_width*win_height * 3);
}


/******************************************************************************
Callback function for dragging mouse
******************************************************************************/

void motion(int x, int y) {
	float r[4];
	float s, t;

	s = (2.0 * x - win_width) / win_width;
	t = (2.0 * (win_height - y) - win_height) / win_height;

	if ((s == s_old) && (t == t_old))
		return;

	switch (mouse_mode) {
	case 2:

		Quaternion rvec;

		mat_to_quat(rotmat, rvec);
		trackball(r, s_old, t_old, s, t);
		add_quats(r, rvec, rvec);
		quat_to_mat(rvec, rotmat);

		s_old = s;
		t_old = t;

		display();
		break;

	case 1:

		translation[0] += (s - s_old);
		translation[1] += (t - t_old);

		s_old = s;
		t_old = t;

		display();
		break;
	}
}

/******************************************************************************
Callback function for mouse clicks
******************************************************************************/

void mouse(int button, int state, int x, int y) {

	int key = glutGetModifiers();

	if (button == GLUT_LEFT_BUTTON || button == GLUT_RIGHT_BUTTON) {
		
		if (state == GLUT_DOWN) {
			float xsize = (float)win_width;
			float ysize = (float)win_height;

			float s = (2.0 * x - win_width) / win_width;
			float t = (2.0 * (win_height - y) - win_height) / win_height;

			s_old = s;
			t_old = t;

			/*translate*/
			if (button == GLUT_LEFT_BUTTON)
			{
				mouse_mode = 1;
			}

			/*rotate*/
			if (button == GLUT_RIGHT_BUTTON)
			{
				mouse_mode = 2;
			}
		}
		else if (state == GLUT_UP) {

			if (button == GLUT_LEFT_BUTTON && key == GLUT_ACTIVE_SHIFT) {  // build up the selection feedback mode

				/*select face*/

				GLuint selectBuf[512];
				GLint hits;
				GLint viewport[4];

				glGetIntegerv(GL_VIEWPORT, viewport);

				glSelectBuffer(win_width, selectBuf);
				(void)glRenderMode(GL_SELECT);

				glInitNames();
				glPushName(0);

				glMatrixMode(GL_PROJECTION);
				glPushMatrix();
				glLoadIdentity();

				/*create 5x5 pixel picking region near cursor location */
				gluPickMatrix((GLdouble)x, (GLdouble)(viewport[3] - y), 1.0, 1.0, viewport);

				set_view(GL_SELECT);
				set_scene(GL_SELECT, poly);
				display_quads(GL_SELECT, poly);

				glMatrixMode(GL_PROJECTION);
				glPopMatrix();
				glFlush();

				glMatrixMode(GL_MODELVIEW);

				hits = glRenderMode(GL_RENDER);
				poly->selected_quad = processHits(hits, selectBuf);
				printf("Selected quad id = %d\n", poly->selected_quad);
				glutPostRedisplay();

			}
			else if (button == GLUT_LEFT_BUTTON && key == GLUT_ACTIVE_CTRL)
			{
				/*select vertex*/

				GLuint selectBuf[512];
				GLint hits;
				GLint viewport[4];

				glGetIntegerv(GL_VIEWPORT, viewport);

				glSelectBuffer(win_width, selectBuf);
				(void)glRenderMode(GL_SELECT);

				glInitNames();
				glPushName(0);

				glMatrixMode(GL_PROJECTION);
				glPushMatrix();
				glLoadIdentity();

				/*  create 5x5 pixel picking region near cursor location */
				gluPickMatrix((GLdouble)x, (GLdouble)(viewport[3] - y), 1.0, 1.0, viewport);

				set_view(GL_SELECT);
				set_scene(GL_SELECT, poly);
				display_vertices(GL_SELECT, poly);

				glMatrixMode(GL_PROJECTION);
				glPopMatrix();
				glFlush();

				glMatrixMode(GL_MODELVIEW);

				hits = glRenderMode(GL_RENDER);
				poly->selected_vertex = processHits(hits, selectBuf);
				printf("Selected vert id = %d\n", poly->selected_vertex);
				glutPostRedisplay();

			}

			mouse_mode = -1;
		}
	}
}

/******************************************************************************
Callback function for mouse wheel scroll
******************************************************************************/

void mousewheel(int wheel, int direction, int x, int y) {
	if (direction == 1) {
		zoom *= zoomspeed;
		glutPostRedisplay();
	}
	else if (direction == -1) {
		zoom /= zoomspeed;
		glutPostRedisplay();
	}
}

/*Display IBFV*/
void makePatterns(void)
{
	pixels = (unsigned char *)malloc(sizeof(unsigned char)*win_width*win_height * 3);
	memset(pixels, 255, sizeof(unsigned char)*win_width*win_height * 3);

	int lut[256];
	int phase[NPN][NPN];
	GLubyte pat[NPN][NPN][4];
	int i, j, k, t;

	for (i = 0; i < 256; i++) lut[i] = i < 127 ? 0 : 255;
	for (i = 0; i < NPN; i++)
		for (j = 0; j < NPN; j++) phase[i][j] = rand() % 256;

	for (k = 0; k < Npat; k++) {
		t = k * 256 / Npat;
		for (i = 0; i < NPN; i++)
			for (j = 0; j < NPN; j++) {
				pat[i][j][0] =
					pat[i][j][1] =
					pat[i][j][2] = lut[(t + phase[i][j]) % 255];
				pat[i][j][3] = int(0.12 * 255);
			}
		glNewList(k + 1, GL_COMPILE);
		glTexImage2D(GL_TEXTURE_2D, 0, 4, NPN, NPN, 0, GL_RGBA, GL_UNSIGNED_BYTE, pat);
		glEndList();
	}

}

void displayIBFV(void)
{
	glDisable(GL_LIGHTING);
	glDisable(GL_LIGHT0);
	glDisable(GL_LIGHT1);
	glDisable(GL_POLYGON_OFFSET_FILL);
	glDisable(GL_DEPTH_TEST);

	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexEnvf(GL_TEXTURE_ENV, GL_TEXTURE_ENV_MODE, GL_REPLACE);

	glEnable(GL_TEXTURE_2D);
	glShadeModel(GL_FLAT);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

	glClearColor(1.0, 1.0, 1.0, 1.0);  // background for rendering color coding and lighting
	glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

	/*draw the model with using the pixels, using vector field to advert the texture coordinates*/
	glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, win_width, win_height, 0, GL_RGB, GL_UNSIGNED_BYTE, pixels);

	double modelview_matrix1[16], projection_matrix1[16];
	int viewport1[4];
	glGetDoublev(GL_MODELVIEW_MATRIX, modelview_matrix1);
	glGetDoublev(GL_PROJECTION_MATRIX, projection_matrix1);
	glGetIntegerv(GL_VIEWPORT, viewport1);

	for (int i = 0; i < poly->nquads; i++) { //go through all the quads

		Quad *temp_q = poly->qlist[i];

		glBegin(GL_QUADS);

		for (int j = 0; j < 4; j++) {
			Vertex *temp_v = temp_q->verts[j];

			double x = temp_v->x;
			double y = temp_v->y;

			double tx, ty, dummy;

			gluProject((GLdouble)temp_v->x, (GLdouble)temp_v->y, (GLdouble)temp_v->z,
				modelview_matrix1, projection_matrix1, viewport1, &tx, &ty, &dummy);

			tx = tx / win_width;
			ty = ty / win_height;

			icVector2 dp = icVector2(temp_v->vx, temp_v->vy);
			normalize(dp);

			double dx = dp.x;
			double dy = dp.y;

			double r = dx * dx + dy * dy;
			if (r > dmax*dmax) {
				r = sqrt(r);
				dx *= dmax / r;
				dy *= dmax / r;
			}

			float px = tx + dx;
			float py = ty + dy;

			glTexCoord2f(px, py);
			glVertex3d(temp_v->x, temp_v->y, temp_v->z);
		}
		glEnd();
	}

	iframe = iframe + 1;

	glEnable(GL_BLEND);

	/*blend the drawing with another noise image*/
	glMatrixMode(GL_PROJECTION);
	glPushMatrix();
	glLoadIdentity();


	glMatrixMode(GL_MODELVIEW);
	glPushMatrix();
	glLoadIdentity();

	glTranslatef(-1.0, -1.0, 0.0);
	glScalef(2.0, 2.0, 1.0);

	glCallList(iframe % Npat + 1);

	glBegin(GL_QUAD_STRIP);

	glTexCoord2f(0.0, 0.0);  glVertex2f(0.0, 0.0);
	glTexCoord2f(0.0, tmax); glVertex2f(0.0, 1.0);
	glTexCoord2f(tmax, 0.0);  glVertex2f(1.0, 0.0);
	glTexCoord2f(tmax, tmax); glVertex2f(1.0, 1.0);
	glEnd();
	glDisable(GL_BLEND);

	glMatrixMode(GL_MODELVIEW);
	glPopMatrix();

	glMatrixMode(GL_PROJECTION);
	glPopMatrix();

	glReadPixels(0, 0, win_width, win_height, GL_RGB, GL_UNSIGNED_BYTE, pixels);


	/*draw the model with using pixels, note the tx and ty do not take the vector on points*/
	glClearColor(1.0, 1.0, 1.0, 1.0);  // background for rendering color coding and lighting
	glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
	glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, win_width, win_height, 0, GL_RGB, GL_UNSIGNED_BYTE, pixels);
	for (int i = 0; i < poly->nquads; i++) { //go through all the quads
		Quad *temp_q = poly->qlist[i];
		glBegin(GL_QUADS);
		for (int j = 0; j < 4; j++) {
			Vertex *temp_v = temp_q->verts[j];
			double x = temp_v->x;
			double y = temp_v->y;
			double tx, ty, dummy;
			gluProject((GLdouble)temp_v->x, (GLdouble)temp_v->y, (GLdouble)temp_v->z,
				modelview_matrix1, projection_matrix1, viewport1, &tx, &ty, &dummy);
			tx = tx / win_width;
			ty = ty / win_height;
			glTexCoord2f(tx, ty);
			glVertex3d(temp_v->x, temp_v->y, temp_v->z);
		}
		glEnd();
	}

	glDisable(GL_TEXTURE_2D);
	glShadeModel(GL_SMOOTH);
	glDisable(GL_BLEND);
}

/******************************************************************************
Callback function for scene display
******************************************************************************/

void display(void)
{
	glClearColor(1.0, 1.0, 1.0, 1.0);  // background for rendering color coding and lighting

	glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

	set_view(GL_RENDER);
	CHECK_GL_ERROR();

	set_scene(GL_RENDER, poly);
	CHECK_GL_ERROR();

	/*display the mesh*/
	display_polyhedron(poly);
	CHECK_GL_ERROR();

	/*display selected elements*/
	display_selected_vertex(poly);
	CHECK_GL_ERROR();

	display_selected_quad(poly);
	CHECK_GL_ERROR();

	glFlush();
	glutSwapBuffers();
	glFinish();

	CHECK_GL_ERROR();
}

/******************************************************************************
Collects a bunch of streamlines in a mesh
******************************************************************************/

void gatherStreamlines() {
	streamlines.clear();

	for (int i = 0; i < poly->nverts; i += 3) {
		int x = poly->vlist[i]->x;
		int y = poly->vlist[i]->y;

		PolyLine line;
		extract_streamline(x, y, 0, line);
		streamlines.push_back(line);
	}
}

/******************************************************************************
Collects a bunch of vectors in a mesh
******************************************************************************/

void gatherVectors() {
	vectors.clear();
	int vertsPerRow = sqrt(poly->nverts);
	double maxScalar = poly->vlist[0]->scalar;

	// get max scalar to use as ratio for vector length normalization
	for (int i = 0; i < poly->nverts; i++)
		if (poly->vlist[i]->scalar > maxScalar)
			maxScalar = poly->vlist[i]->scalar;

	// gather vectors
	// only doing the interior rows (ignoring outer ring)
	for (int i = vertsPerRow + 1; i < poly->nverts - vertsPerRow - 1; i++) {
		if ((i % vertsPerRow != 0) && ((i + 1) % vertsPerRow != 0)) {
			Vertex* v = poly->vlist[i];

			if (v->scalar > 1) {
				double vLen = (v->scalar / maxScalar) * 1.5;

				icVector3 dir = getDir(v->x, v->y, v->z);
				normalize(dir);
				icVector3 start = icVector3(v->x, v->y, v->z);
				icVector3 end = icVector3(v->x + (dir.x * vLen), v->y + (dir.y * vLen), v->x + (dir.y * vLen));

				LineSegment line = LineSegment(start, end);
				vectors.push_back(line);
			}
		}
	}
}

/******************************************************************************
Custom function for finding the direction at a specific point
******************************************************************************/

icVector3 getDir(double x, double y, double z) {
	Quad* q = NULL;
	double x1, x2, y1, y2;

	// find quad the point's in
	for (int i = 0; i < poly->nquads; i++) {
		q = poly->qlist[i];

		/* find x1, x2, y1, y2
		   [1] . . . [0]
			.         .
			.         .
			.         .
		   [2] . . . [3]
		*/
		x1 = q->verts[2]->x;
		y1 = q->verts[2]->y;
		x2 = q->verts[0]->x;
		y2 = q->verts[0]->y;

		if (x <= x2 && x >= x1 && y <= y2 && y >= y1)
			break; // found
	}

	// find k (usually just 2)
	int k = -1;
	for (int i = 0; i < 4; i++) {
		if (q->verts[i]->x == x1 && q->verts[i]->y == y1)
			k = i;
	}

	// find dirX
	double fx1y1 = q->verts[k]->vx;
	double fx2y1 = q->verts[(k + 1) % 4]->vx;
	double fx2y2 = q->verts[(k + 2) % 4]->vx;
	double fx1y2 = q->verts[(k + 3) % 4]->vx;

	double p1 = ((x2 - x) / (x2 - x1)) * ((y2 - y) / (y2 - y1));
	double p2 = ((x - x1) / (x2 - x1)) * ((y2 - y) / (y2 - y1));
	double p3 = ((x2 - x) / (x2 - x1)) * ((y - y1) / (y2 - y1));
	double p4 = ((x - x1) / (x2 - x1)) * ((y - y1) / (y2 - y1));

	double dirX = (p1 * fx1y1) + (p2 * fx2y1) + (p3 * fx1y2) + (p4 * fx2y2);

	// find dirY
	fx1y1 = q->verts[k]->vy;
	fx2y1 = q->verts[(k + 1) % 4]->vy;
	fx2y2 = q->verts[(k + 2) % 4]->vy;
	fx1y2 = q->verts[(k + 3) % 4]->vy;

	double dirY = (p1 * fx1y1) + (p2 * fx2y1) + (p3 * fx1y2) + (p4 * fx2y2);

	return icVector3(dirX, dirY, 0);
}

/******************************************************************************
Custom function for extracting a files streamline
******************************************************************************/

void extract_streamline(double x, double y, double z, PolyLine& contour) {
	double step = .25;
	int count = 1500;
	double c_x = x;
	double c_y = y;
	double c_z = z;

	// trace forward
	for (int i = 0; i < count; i++) {
		// part 1
		if (c_x > poly->maxx || c_x < poly->minx || c_y > poly->maxy || c_y < poly->miny)
			continue;

		// part 2
		icVector3 start = icVector3(c_x, c_y, c_z);
		icVector3 dir = getDir(c_x, c_y, c_z);
		normalize(dir);
		icVector3 end = icVector3((c_x + (dir.x * step)), (c_y + (dir.y * step)), (c_z + (dir.z * step)));

		// part 3
		c_x = end.x;
		c_y = end.y;
		c_z = end.z;

		// part 4
		if (c_x > poly->maxx || c_x < poly->minx || c_y > poly->maxy || c_y < poly->miny)
			break;

		LineSegment line = LineSegment(start, end);
		contour.push_back(line);
	}

	// reset
	c_x = x;
	c_y = y;
	c_z = z;

	// trace backward
	for (int i = 0; i < count; i++) {
		// part 1
		if (c_x > poly->maxx || c_x < poly->minx || c_y > poly->maxy || c_y < poly->miny)
			continue;

		// part 2
		icVector3 start = icVector3(c_x, c_y, c_z);
		icVector3 dir = -getDir(c_x, c_y, c_z);
		normalize(dir);
		icVector3 end = icVector3((c_x + dir.x * step), (c_y + dir.y * step), (c_z + dir.z * step));

		// part 3
		c_x = end.x;
		c_y = end.y;
		c_z = end.z;

		// part 4
		if (c_x > poly->maxx || c_x < poly->minx || c_y > poly->maxy || c_y < poly->miny)
			break;

		LineSegment line = LineSegment(start, end);
		contour.push_back(line);
	}
}

/******************************************************************************
Process a keyboard action.  In particular, exit the program when an
"escape" is pressed in the window.
******************************************************************************/

/*global variable to save polylines*/
PolyLine pentagon;

void keyboard(unsigned char key, int x, int y) {
	/* set escape key to exit */
	switch (key) {
	case 27:
		poly->finalize();  // finalize_everything
		exit(0);
		break;

	case '1':
		display_mode = 1;
		glutPostRedisplay();
		break;

	case '2':
		display_mode = 2;
		glutPostRedisplay();
		break;

	case '3':
	{
		display_mode = 3;

		double L = (poly->radius * 2) / 30;
		for (int i = 0; i < poly->nquads; i++) {
			Quad* temp_q = poly->qlist[i];
			for (int j = 0; j < 4; j++) {

				Vertex* temp_v = temp_q->verts[j];

				temp_v->R = int(temp_v->x / L) % 2 == 0 ? 1 : 0;
				temp_v->G = int(temp_v->y / L) % 2 == 0 ? 1 : 0;
				temp_v->B = 0.0;
			}
		}
		glutPostRedisplay();
	}
	break;
	case '4':
		display_mode = 4;
		{
			//examples for dot drawing and polyline drawing

			//create a polylines of a pentagon
			//clear current polylines
			pentagon.clear();
			//there are five vertices of a pentagon
			//the angle of each edge is 2pi/5.0
			double da = 2.0*PI / 5.0;
			for (int i = 0; i < 5; i++) {
				double angle = i * da;
				double cx = cos(angle);
				double cy = sin(angle);

				double n_angle = (i + 1) % 5 * da;
				double nx = cos(n_angle);
				double ny = sin(n_angle);

				LineSegment line(cx, cy, 0, nx, ny, 0);
				pentagon.push_back(line);
			}

		}
		glutPostRedisplay();
		break;

	case '5':
		display_mode = 5;
		//show the IBFV of the field
		break;
      
  case '6':
		display_mode = 6;
		glutPostRedisplay();
		break;

	// vector field
	case '7':
		display_mode = 7;
		if (!vectors.size())
			gatherVectors();
		glutPostRedisplay();
		break;

	// streamlines (broken)
	case '8':
		display_mode = 8;
		if (!streamlines.size())
			gatherStreamlines();
		glutPostRedisplay();
		break;

    // Increment the load
	case 'x': {
		poly->finalize();
		load_selector = (load_selector + 1) % LOADABLE_COUNT;
		char buffer[256];
		strcpy(buffer, LOAD_PATHS[load_selector]);
		load_ply(buffer);
		poly->initialize(); // initialize the mesh
		// poly->write_info();
		makePatterns();
		if (display_mode == 7) gatherVectors();
		if (display_mode == 8) gatherStreamlines();
		printf("Loaded set %d (%s).\n", load_selector, buffer);
		
	}
	break;

	case 'r':
		mat_ident(rotmat);
		translation[0] = 0;
		translation[1] = 0;
		zoom = 1.0;
		glutPostRedisplay();
		break;
	}
}


/******************************************************************************
Diaplay the polygon with visualization results
******************************************************************************/


void display_polyhedron(Polyhedron* poly)
{
	glEnable(GL_POLYGON_OFFSET_FILL);
	glPolygonOffset(1., 1.);

	glEnable(GL_DEPTH_TEST);
	glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
	glShadeModel(GL_SMOOTH);
	CHECK_GL_ERROR();

	double lower, upper;
	scalar_bounds(poly, &lower, &upper); // Find bounds

	switch (display_mode) {
		case 1:
		{
			glEnable(GL_LIGHTING);
			glEnable(GL_LIGHT0);
			glEnable(GL_LIGHT1);

			glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
			GLfloat mat_diffuse[4] = { 1.0, 1.0, 0.0, 0.0 };
			GLfloat mat_specular[] = { 1.0, 1.0, 1.0, 1.0 };
			glMaterialfv(GL_FRONT, GL_DIFFUSE, mat_diffuse);
			glMaterialfv(GL_FRONT, GL_SPECULAR, mat_specular);
			glMaterialf(GL_FRONT, GL_SHININESS, 50.0);

			for (int i = 0; i < poly->nquads; i++) {
				Quad* temp_q = poly->qlist[i];
				glBegin(GL_POLYGON);
				for (int j = 0; j < 4; j++) {
					Vertex* temp_v = temp_q->verts[j];
					glNormal3d(temp_v->normal.entry[0], temp_v->normal.entry[1], temp_v->normal.entry[2]);
					glVertex3d(temp_v->x, temp_v->y, temp_v->z);
				}
				glEnd();
			}

			CHECK_GL_ERROR();
		}
		break;

		case 2:
		{
			glDisable(GL_LIGHTING);
			glEnable(GL_LINE_SMOOTH);
			glHint(GL_LINE_SMOOTH_HINT, GL_NICEST);
			glEnable(GL_BLEND);
			glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
			glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
			glLineWidth(1.0);
			for (int i = 0; i < poly->nquads; i++) {
				Quad* temp_q = poly->qlist[i];

				glBegin(GL_POLYGON);
				for (int j = 0; j < 4; j++) {
					Vertex* temp_v = temp_q->verts[j];
					glNormal3d(temp_q->normal.entry[0], temp_q->normal.entry[1], temp_q->normal.entry[2]);
					glColor3f(0.0, 0.0, 0.0);
					glVertex3d(temp_v->x, temp_v->y, temp_v->z);
				}
				glEnd();

			}

			glDisable(GL_BLEND);
		}
		break;

		case 3:
			glDisable(GL_LIGHTING);
			for (int i = 0; i < poly->nquads; i++) {
				Quad* temp_q = poly->qlist[i];
				glBegin(GL_POLYGON);
				for (int j = 0; j < 4; j++) {
					Vertex* temp_v = temp_q->verts[j];
					glColor3f(temp_v->R, temp_v->G, temp_v->B);
					glVertex3d(temp_v->x, temp_v->y, temp_v->z);
				}
				glEnd();
			}
			break;

		case 4:
		{
			//draw a dot at position (0.2, 0.3, 0.4) 
			//with radius 0.1 in color blue(0.0, 0.0, 1.0)
			drawDot(0.2, 0.3, 0.4, 0.1, 0.0, 0.0, 1.0);

			//draw a dot at position of vlist[110]
			//with radius 0.2 in color magenta (1.0, 0.0, 1.0)
			Vertex *v = poly->vlist[110];
			drawDot(v->x, v->y, v->z, 0.2, 1.0, 0.0, 1.0);

			//draw line segment start at vlist[110] and end at (vlist[135]->x, vlist[135]->y, 4)
			//with color (0.02, 0.1, 0.02) and width 1
			LineSegment line(poly->vlist[110]->x, poly->vlist[110]->y, poly->vlist[110]->z,
				poly->vlist[135]->x, poly->vlist[135]->y, 4);
			drawLineSegment(line, 1.0, 0.0, 1.0, 0.0);

			//draw a polyline of pentagon with color orange(1.0, 0.5, 0.0) and width 2
			drawPolyline(pentagon, 2.0, 1.0, 0.5, 0.0);

			//display the mesh with color cyan (0.0, 1.0, 1.0)
			glDisable(GL_LIGHTING);
			for (int i = 0; i < poly->nquads; i++) {
				Quad* temp_q = poly->qlist[i];
				glBegin(GL_POLYGON);
				for (int j = 0; j < 4; j++) {
					Vertex* temp_v = temp_q->verts[j];
					glColor3f(0.0, 1.0, 1.0);
					glVertex3d(temp_v->x, temp_v->y, temp_v->z);
				}
				glEnd();
			}
		}
		break;

		case 5:
			displayIBFV();
			break;

		case 6: {
			float red[3]  = { 1.0, 0.0, 0.0 };
			float blue[3] = { 0.0, 0.0, 1.0 };

			for (int i = 0; i < poly->nquads; i++) {
				Quad* temp_q = poly->qlist[i];
				display_bicolor_quad(temp_q, lower, upper, red, blue);
			}
		}
		break;
		
		case 7: {
			for (int i = 0; i < vectors.size(); i++)
				drawLineSegment(vectors.at(i), 0.5, 1, 1, 1);
			//drawDot(vectors.at(i)->x, vectors.at(i)->y, 0, 0.25, 1, 1, 1);

			glDisable(GL_LIGHTING);
			for (int i = 0; i < poly->nquads; i++) {
				Quad* temp_q = poly->qlist[i];
				glBegin(GL_POLYGON);
				for (int j = 0; j < 4; j++) {
					Vertex* temp_v = temp_q->verts[j];
					glColor3f(0.0, 0.0, 0.0);
					glVertex3d(temp_v->x, temp_v->y, temp_v->z);
				}
				glEnd();
			}
		}
		break;

		case 8: {
			for (int i = 0; i < streamlines.size(); i++)
				drawPolyline(streamlines[i], 1, 1, 1, 1);

			glDisable(GL_LIGHTING);
			for (int i = 0; i < poly->nquads; i++) {
				Quad* temp_q = poly->qlist[i];
				glBegin(GL_POLYGON);
				for (int j = 0; j < 4; j++) {
					Vertex* temp_v = temp_q->verts[j];
					glColor3f(0.0, 0.0, 0.0);
					glVertex3d(temp_v->x, temp_v->y, temp_v->z);
				}
				glEnd();
			}
		}
		break;
	}
}

/******************************************************************************
Assignment methods
******************************************************************************/

void load_ply(char* ply_path) {
	FILE* this_file = fopen(ply_path, "r");
	if (this_file == NULL)
		throw EXCEPTION_READ_FAULT;
	poly = new Polyhedron(this_file);
	fclose(this_file);
}

// Scalar fields

void scalar_bounds(Polyhedron* poly, double* lower, double* upper)
{
	*lower = poly->vlist[0]->scalar;
	*upper = poly->vlist[0]->scalar;
	for (int i = 1; i < poly->nverts; i++) {
		if (poly->vlist[i]->scalar < *lower)
			*lower = poly->vlist[i]->scalar;
		if (poly->vlist[i]->scalar > *upper)
			*upper = poly->vlist[i]->scalar;
	}
}

/*
* Quad rendering. Useful reference:
* glColor3f(temp_v->R, temp_v->G, temp_v->B);
* glVertex3d(temp_v->x, temp_v->y, temp_v->z);
*/

/// <summary>
/// Displays a quadrilateal to the screen, formatted in grayscale by its scalar data
/// </summary>
/// <param name="qu">The quadrilateral to display</param>
/// <param name="lower">The minimum value considered "black", generally taken from the mesh the quad was extracted from</param>
/// <param name="upper">The maximum value considered "white", generally taken from the mesh the quad was extracted from</param>
void display_grayscale_quad(Quad* qu, double lower, double upper) {
	// I could bear to define const color aliases, but for now this will do.
	float black[3] = { 0, 0, 0 };
	float white[3] = { 1, 1, 1 };
	display_bicolor_quad(qu, lower, upper, black, white);
}

/// <summary>
/// Displays a quadrilateal to the screen, formatted in pretty colors by its scalar data
/// </summary>
/// <param name="qu">The quadrilateral to display</param>
/// <param name="lower">The minimum value considered "lower_color", generally taken from the mesh the quad was extracted from</param>
/// <param name="upper">The maximum value considered "upper_color", generally taken from the mesh the quad was extracted from</param>
/// <param name="lower_color">The color to associate with low scalar values</param>
/// <param name="upper_color">The color to associate with high scalar values</param>
void display_bicolor_quad(Quad* qu, double lower, double upper, float lower_color[3], float upper_color[3]) {
	display_bicolor_heightmod_quad(qu, lower, upper, lower_color, upper_color, 0);
}

/// <summary>
/// Displays a quad with its magnitude set in the z direction proportional to the scaler.
/// </summary>
/// <param name="qu">The quadrilateral to display</param>
/// <param name="lower">The scaler value to consider "zero" in the projection sense.</param>
/// <param name="upper">The scaler value to consider "very high" in the projection sense.</param>
void display_heightmod_quad(Quad* qu, double lower, double upper, float ref_color[3], float peak) {
	//float ref_color[3] = { qu->verts[0]->R, qu->verts[0]->G, qu->verts[0]->B };
	display_bicolor_heightmod_quad(qu, lower, upper, ref_color, ref_color, peak);
}

/// <summary>
/// Displays a quad with its magnitude multiplied by its scaler in a single direction and shaded in grayscale.<br/>
/// No, I don't mean shaders, that's another can of worms.
/// </summary>
/// <param name="qu">The quadrilateral to display</param>
/// <param name="lower">The scaler value to consider "zero" in the projection sense and "black" in the color sense.</param>
/// <param name="upper">The scaler value to consider "very high" in the projection sense and "white" in the color sense.</param>
void display_grayscale_heightmod_quad(Quad* qu, double lower, double upper, float peak) {
	float black[3] = { 0, 0, 0 };
	float white[3] = { 1, 1, 1 };
	display_bicolor_heightmod_quad(qu, lower, upper, black, white, peak);
}

/// <summary>
/// Displays a quad with its magnitude multiplied by its scaler in a single direction and colored.<br/>
/// We assume all datasets have a fixed z coordinate, so we use this to demonstrate height mapping.
/// </summary>
/// <param name="qu">The quadrilateral to display</param>
/// <param name="lower">The scaler value to consider "zero" in the projection sense and "lower_color" in the color sense.</param>
/// <param name="upper">The scaler value to consider "very high" in the projection sense and "upper_color" in the color sense.</param>
/// <param name="lower_color">The color to associate with low scalar values</param>
/// <param name="upper_color">The color to associate with high scalar values</param>
void display_bicolor_heightmod_quad(Quad* qu, double lower, double upper, float lower_color[3], float upper_color[3], float peak) {
	unsigned int i;

	glBegin(GL_POLYGON);
	for (i = 0; i < 4; i++) {
		Vertex* ve = qu->verts[i];
		double sca = ve->scalar;

		// Part 1: Color
		float interlopated_color[3] = { 0,0,0 };
		for (int j = 0; j < 3; j++)
			interlopated_color[j] = lower_color[j] * ((sca - lower) / (upper - lower))
			+ upper_color[j] * ((upper - sca) / (upper - lower));
		glColor3f(interlopated_color[0], interlopated_color[1], interlopated_color[2]);
		//printf("%f,%f,%f\n", interlopated_color[0], interlopated_color[1], interlopated_color[2]);
		// Part 2: Location
		float interlopated_height = peak * ((sca - lower) / (upper - lower));
		glVertex3d(ve->x, ve->y, interlopated_height);
	}
	glEnd();
}