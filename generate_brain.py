import bpy
import bmesh
import math
import os

def clear_scene():
    """Clears all default objects in the scene to start fresh."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    # Remove unused materials and textures
    for material in bpy.data.materials:
        bpy.data.materials.remove(material)
    for texture in bpy.data.textures:
        bpy.data.textures.remove(texture)

def create_brain_material():
    """Creates a premium organic brain tissue material using Subsurface Scattering (SSS)."""
    material = bpy.data.materials.new(name="Brain_Tissue")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    
    # Clear default nodes
    for node in list(nodes):
        nodes.remove(node)
        
    # Create Principled BSDF & Output nodes
    shader = nodes.new(type='ShaderNodeBsdfPrincipled')
    output = nodes.new(type='ShaderNodeOutputMaterial')
    
    # Configure organic flesh look
    # Base Color: Warm soft pinkish-grey/peach
    shader.inputs['Base Color'].default_value = (0.9, 0.76, 0.72, 1.0)
    # Subsurface weight
    shader.inputs['Subsurface Weight'].default_value = 0.35
    # Subsurface radius (Red scattered further, typical of skin/flesh)
    shader.inputs['Subsurface Radius'].default_value = (1.2, 0.8, 0.6)
    # Roughness (moist surface)
    shader.inputs['Roughness'].default_value = 0.22
    shader.inputs['IOR'].default_value = 1.333  # IOR of water/fluid
    
    # Link nodes
    links.new(shader.outputs['BSDF'], output.inputs['Surface'])
    return material

def create_brainstem_material():
    """Creates a slightly distinct, more fibrous material for the brainstem."""
    material = bpy.data.materials.new(name="Brainstem_Tissue")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    
    for node in list(nodes):
        nodes.remove(node)
        
    shader = nodes.new(type='ShaderNodeBsdfPrincipled')
    output = nodes.new(type='ShaderNodeOutputMaterial')
    
    # Brainstem is slightly more white/yellowish, less vascularized surface gyri
    shader.inputs['Base Color'].default_value = (0.92, 0.84, 0.78, 1.0)
    shader.inputs['Subsurface Weight'].default_value = 0.25
    shader.inputs['Subsurface Radius'].default_value = (1.0, 0.7, 0.5)
    shader.inputs['Roughness'].default_value = 0.3
    
    links.new(shader.outputs['BSDF'], output.inputs['Surface'])
    return material

def generate_hemisphere(name, is_left=True):
    """Generates a highly-detailed, mathematically displaced hemisphere of the cerebrum."""
    # Create high-resolution UV sphere as starting geometry
    # A high poly count prevents "low poly trash"!
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=128,
        ring_count=64,
        radius=1.0,
        location=(0, 0, 0)
    )
    obj = bpy.context.active_object
    obj.name = name
    
    # Access mesh data
    mesh = obj.data
    bpy.ops.object.mode_set(mode='OBJECT')
    
    # Brain shape scale parameters
    scale_x = 0.85
    scale_y = 1.15
    scale_z = 0.8
    
    # Separate left and right hemispheres laterally
    lateral_shift = -0.06 if is_left else 0.06
    
    for vert in mesh.vertices:
        # 1. Base shape transformation (ellipsoidal cerebrum shape)
        x = vert.co.x * scale_x + lateral_shift
        y = vert.co.y * scale_y
        z = vert.co.z * scale_z
        
        # 2. Mathematical Gyri & Sulci folding pattern
        # Volumetric mathematical displacement using sine & cosine modulation
        w1 = 12.0
        w2 = 6.0
        
        # We calculate the angle and distance to make displacements surface-normal oriented
        dist = math.sqrt(x**2 + y**2 + z**2)
        nx = x / dist if dist > 0 else 0
        ny = y / dist if dist > 0 else 0
        nz = z / dist if dist > 0 else 0
        
        # Base brain gyri noise formula
        fold = (
            math.sin(w1 * x + math.sin(w2 * y)) *
            math.cos(w1 * y + math.sin(w2 * z)) *
            math.sin(w1 * z + math.sin(w2 * x))
        )
        
        # Secondary high-frequency detailing octave
        w3 = 24.0
        fold += 0.35 * (
            math.sin(w3 * x) * math.cos(w3 * y) * math.sin(w3 * z)
        )
        
        # Scale the folding height
        amplitude = 0.065
        displacement = fold * amplitude
        
        # 3. Anatomical Fissure Shaping:
        # Fissure constraint: Suppress displacement on the medial flat plane (x near 0)
        # to form a realistic deep longitudinal fissure where hemispheres face each other.
        medial_suppression = 1.0 - math.exp(-12.0 * abs(vert.co.x))
        displacement *= medial_suppression
        
        # 4. Temporal lobe and anterior/posterior tapering refinement
        # Flatten bottom slightly, taper the front (anterior is +y), expand the back (posterior is -y)
        shape_taper = 1.0
        if y > 0:
            shape_taper = 1.0 - 0.15 * (y / scale_y)  # taper front
        else:
            shape_taper = 1.0 + 0.05 * (abs(y) / scale_y)  # broaden back
            
        # Apply the final positions
        vert.co.x = (vert.co.x * scale_x + displacement * nx) * shape_taper + lateral_shift
        vert.co.y = (vert.co.y * scale_y + displacement * ny) * shape_taper
        vert.co.z = (vert.co.z * scale_z + displacement * nz) * shape_taper
        
    # Update geometry and apply smooth shading
    mesh.update()
    bpy.ops.object.shade_smooth()
    
    # Add Subdivision Surface modifier for absolute flawless smoothness
    subsurf = obj.modifiers.new(name="Subdivision", type='SUBSURF')
    subsurf.levels = 1
    subsurf.render_levels = 2
    
    return obj

def generate_cerebellum():
    """Generates the cerebellum positioned posterior-inferiorly with horizontal ribbed folds."""
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=96,
        ring_count=48,
        radius=1.0,
        location=(0, -0.65, -0.45)
    )
    obj = bpy.context.active_object
    obj.name = "Cerebellum"
    mesh = obj.data
    
    # Scale parameters for a smaller, wider posterior cerebellum
    scale_x = 0.75
    scale_y = 0.5
    scale_z = 0.45
    
    for vert in mesh.vertices:
        x = vert.co.x * scale_x
        y = vert.co.y * scale_y
        z = vert.co.z * scale_z
        
        dist = math.sqrt(x**2 + y**2 + z**2)
        nx = x / dist if dist > 0 else 0
        ny = y / dist if dist > 0 else 0
        nz = z / dist if dist > 0 else 0
        
        # Cerebellum has dense, horizontal, parallel folia (ribbed folds)
        folia_freq = 42.0
        folia = math.sin(folia_freq * z) * math.cos(6.0 * x)
        displacement = folia * 0.03
        
        vert.co.x = vert.co.x * scale_x + displacement * nx
        vert.co.y = vert.co.y * scale_y + displacement * ny - 0.65
        vert.co.z = vert.co.z * scale_z + displacement * nz - 0.45
        
    mesh.update()
    bpy.ops.object.shade_smooth()
    
    subsurf = obj.modifiers.new(name="Subdivision", type='SUBSURF')
    subsurf.levels = 1
    
    return obj

def generate_brainstem():
    """Generates the brainstem extending downwards from the center-bottom."""
    # Start with a high-poly cylinder
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=64,
        radius=0.2,
        depth=0.8,
        location=(0, -0.25, -0.85)
    )
    obj = bpy.context.active_object
    obj.name = "Brainstem"
    mesh = obj.data
    
    for vert in mesh.vertices:
        # Add slight tapering down the stem (negative z)
        taper = 1.0 - 0.3 * (vert.co.z + 0.4)
        vert.co.x *= taper
        vert.co.y *= taper
        
        # Curve it slightly forward organically (anterior is +y)
        vert.co.y += 0.1 * math.cos(vert.co.z * math.pi)
        
        # Add subtle vertical fiber tracts
        striations = 0.005 * math.sin(24.0 * vert.co.x) * math.sin(24.0 * vert.co.y)
        vert.co.x += striations
        vert.co.y += striations
        
    mesh.update()
    bpy.ops.object.shade_smooth()
    
    subsurf = obj.modifiers.new(name="Subdivision", type='SUBSURF')
    subsurf.levels = 1
    
    return obj

def setup_studio_lighting_and_scene():
    """Sets up a highly professional cinematic studio lighting stage to showcase the model."""
    # 1. Add Dark Reflective Ground Plate
    bpy.ops.mesh.primitive_plane_add(size=20, location=(0, 0, -2))
    ground = bpy.context.active_object
    ground.name = "Studio_Floor"
    
    # Ground material (sleek matte black/dark grey reflective)
    ground_mat = bpy.data.materials.new(name="Studio_Floor_Mat")
    ground_mat.use_nodes = True
    nodes = ground_mat.node_tree.nodes
    nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.015, 0.015, 0.015, 1.0)
    nodes['Principled BSDF'].inputs['Roughness'].default_value = 0.18
    ground.data.materials.append(ground_mat)
    
    # 2. Add Three-Point Lighting
    # A. Key Light (Soft Warm Studio Spot)
    bpy.ops.object.light_add(type='SPOT', radius=1.0, location=(3, 3, 3))
    key_light = bpy.context.active_object
    key_light.name = "Key_Light"
    key_light.data.energy = 500  # Watts
    key_light.data.color = (1.0, 0.94, 0.88)  # Warm white
    
    # Point key light to center
    constraint = key_light.constraints.new(type='TRACK_TO')
    constraint.target = bpy.data.objects.get("Cerebellum") or bpy.context.scene.objects[0]
    constraint.track_axis = 'TRACK_NEGATIVE_Z'
    constraint.up_axis = 'UP_Y'
    
    # B. Fill Light (Soft Cool Blue Area Light to balance shadows)
    bpy.ops.object.light_add(type='AREA', radius=2.0, location=(-4, -2, 1))
    fill_light = bpy.context.active_object
    fill_light.name = "Fill_Light"
    fill_light.data.energy = 150
    fill_light.data.color = (0.8, 0.88, 1.0)  # Soft cool blue
    
    constraint = fill_light.constraints.new(type='TRACK_TO')
    constraint.target = bpy.data.objects.get("Cerebellum") or bpy.context.scene.objects[0]
    constraint.track_axis = 'TRACK_NEGATIVE_Z'
    constraint.up_axis = 'UP_Y'
    
    # C. Rim Light (Backlight to pop the brain outlines out of the dark background)
    bpy.ops.object.light_add(type='SPOT', radius=0.8, location=(-2, -4, 2))
    rim_light = bpy.context.active_object
    rim_light.name = "Rim_Light"
    rim_light.data.energy = 450
    rim_light.data.color = (1.0, 0.65, 0.45)  # Strong warm amber/rim glow
    
    constraint = rim_light.constraints.new(type='TRACK_TO')
    constraint.target = bpy.data.objects.get("Cerebellum") or bpy.context.scene.objects[0]
    constraint.track_axis = 'TRACK_NEGATIVE_Z'
    constraint.up_axis = 'UP_Y'
    
    # 3. Add Cinematic Camera
    bpy.ops.object.camera_add(location=(3.2, -3.2, 1.8))
    camera = bpy.context.active_object
    camera.name = "Cinematic_Camera"
    camera.data.lens = 55  # 55mm focal length for professional look
    
    # Track camera to center
    cam_constraint = camera.constraints.new(type='TRACK_TO')
    cam_constraint.target = bpy.data.objects.get("Cerebellum") or bpy.context.scene.objects[0]
    cam_constraint.track_axis = 'TRACK_NEGATIVE_Z'
    cam_constraint.up_axis = 'UP_Y'
    
    bpy.context.scene.camera = camera

def main():
    print("-----------------------------------------------------")
    print("[3D Brain Generation] Initiating morphogenesis...")
    print("-----------------------------------------------------")
    
    clear_scene()
    
    # Create Materials
    brain_material = create_brain_material()
    stem_material = create_brainstem_material()
    
    # Generate brain lobes (Hemispheres)
    left_hem = generate_hemisphere("Cerebrum_Left", is_left=True)
    left_hem.data.materials.append(brain_material)
    
    right_hem = generate_hemisphere("Cerebrum_Right", is_left=False)
    right_hem.data.materials.append(brain_material)
    
    # Generate Cerebellum
    cerebellum = generate_cerebellum()
    cerebellum.data.materials.append(brain_material)
    
    # Generate Brainstem
    brainstem = generate_brainstem()
    brainstem.data.materials.append(stem_material)
    
    # Group all under a common parent
    bpy.ops.object.select_all(action='DESELECT')
    left_hem.select_set(True)
    right_hem.select_set(True)
    cerebellum.select_set(True)
    brainstem.select_set(True)
    
    # Add empty parent
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, -0.2, -0.2))
    brain_parent = bpy.context.active_object
    brain_parent.name = "Human_Brain"
    
    left_hem.parent = brain_parent
    right_hem.parent = brain_parent
    cerebellum.parent = brain_parent
    brainstem.parent = brain_parent
    
    # Stage lights and camera
    setup_studio_lighting_and_scene()
    
    # Set Render Properties
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE_NEXT' if hasattr(bpy.types, "RenderEngineEEVEENext") else 'BLENDER_EEVEE'
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100
    
    # Set color management to filmic/sRGB for premium dynamic range
    scene.display_settings.display_device = 'sRGB'
    scene.view_settings.view_transform = 'AgX' if hasattr(scene.view_settings, "view_transform") and 'AgX' in scene.view_settings.bl_rna.properties['view_transform'].enum_items else 'Filmic'
    scene.view_settings.look = 'High Contrast'
    
    # Save blend file
    output_blend = os.path.join(os.getcwd(), "human_brain.blend")
    bpy.ops.wm.save_as_mainfile(filepath=output_blend)
    print(f"[3D Brain Generation] Successfully saved Blender file: {output_blend}")
    
    # Render preview
    preview_path = os.path.join(os.getcwd(), "brain_preview.png")
    scene.render.filepath = preview_path
    print(f"[3D Brain Generation] Rendering cinematic 1080p preview to: {preview_path} ...")
    bpy.ops.render.render(write_still=True)
    print("[3D Brain Generation] Rendering complete!")
    print("-----------------------------------------------------")

if __name__ == "__main__":
    main()
