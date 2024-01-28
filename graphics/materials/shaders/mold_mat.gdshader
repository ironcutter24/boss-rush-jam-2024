shader_type spatial;
render_mode specular_toon;

uniform vec4 color = vec4(0.1, 0.1, 0.2, 1.0);
uniform float roughness = 0.4;
uniform float height_scale = 0.1;
uniform sampler2D noise;
uniform sampler2D normalmap;

varying vec2 tex_position;

void vertex() {
	// Called for every vertex the material is visible on.
	
	tex_position = VERTEX.xz * 0.5 + 0.5;
	float height = texture(noise, tex_position + TIME * .15).x;
	float mult = sin(TIME * 3.0) * 0.5 + 0.5;
	
	VERTEX += NORMAL * height * (height_scale + mult * .06);
}

void fragment() {
	// Called for every pixel the material is visible on.
	
	ALBEDO = color.xyz;
	NORMAL_MAP = texture(normalmap, tex_position).xyz;
	ROUGHNESS = roughness;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
