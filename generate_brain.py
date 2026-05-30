import bpy
import bmesh
import math
import os

def clear_scene():
    """Clears all default objects, materials, and textures in the scene to start fresh."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    # Remove unused materials and textures
    for material in bpy.data.materials:
        bpy.data.materials.remove(material)
    for texture in bpy.data.textures:
        bpy.data.textures.remove(texture)

def set_active_and_selected(obj):
    """Guarantees that the specified object is selected and set as the active object in the scene context."""
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

def optimize_mesh_density(obj, ratio=0.15):
    """Applies a decimate modifier to reduce face count while preserving detailed contours for high-performance WebGL."""
    set_active_and_selected(obj)
    decimate = obj.modifiers.new(name="Decimate_Opt", type='DECIMATE')
    decimate.ratio = ratio
    bpy.ops.object.modifier_apply(modifier=decimate.name)

def create_tissue_material(name, base_color, roughness=0.3, sss_weight=0.3, metallic=0.0, ior=1.333):
    """Utility to create a premium organic material with Subsurface Scattering."""
    material = bpy.data.materials.new(name=name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    
    for node in list(nodes):
        nodes.remove(node)
        
    shader = nodes.new(type='ShaderNodeBsdfPrincipled')
    output = nodes.new(type='ShaderNodeOutputMaterial')
    
    # Configure organic flesh look
    shader.inputs['Base Color'].default_value = base_color
    shader.inputs['Subsurface Weight'].default_value = sss_weight
    shader.inputs['Subsurface Radius'].default_value = (1.2, 0.8, 0.6)  # Red scattering
    shader.inputs['Roughness'].default_value = roughness
    shader.inputs['Metallic'].default_value = metallic
    shader.inputs['IOR'].default_value = ior
    
    links.new(shader.outputs['BSDF'], output.inputs['Surface'])
    return material

def generate_hemisphere(name, is_left=True):
    """Generates a highly-detailed, anatomically accurate cerebral hemisphere using shape-fusing, modifiers, and decimation."""
    lat_sign = -1 if is_left else 1
    
    # 1. Create lobe primitives forming a realistic cerebral base shape
    # Frontal Lobe
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=(lat_sign * 0.08, 0.42, 0.22))
    front = bpy.context.active_object
    front.name = "Front"
    front.scale = (0.55, 0.65, 0.5)
    
    # Parietal Lobe
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=(lat_sign * 0.08, -0.15, 0.35))
    parietal = bpy.context.active_object
    parietal.name = "Parietal"
    parietal.scale = (0.58, 0.65, 0.52)
    
    # Occipital Lobe
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=(lat_sign * 0.08, -0.68, -0.05))
    occipital = bpy.context.active_object
    occipital.name = "Occipital"
    occipital.scale = (0.5, 0.52, 0.45)
    occipital.rotation_euler = (0.15, 0, 0)
    
    # Temporal Lobe (curves forward and down)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=(lat_sign * 0.38, 0.18, -0.22))
    temporal = bpy.context.active_object
    temporal.name = "Temporal"
    temporal.scale = (0.48, 0.6, 0.44)
    temporal.rotation_euler = (0.35, 0, lat_sign * 0.25)
    
    # 2. Join the lobes into a single hemispheric base
    bpy.ops.object.select_all(action='DESELECT')
    front.select_set(True)
    parietal.select_set(True)
    occipital.select_set(True)
    temporal.select_set(True)
    bpy.context.view_layer.objects.active = front
    bpy.ops.object.join()
    
    obj = bpy.context.active_object
    obj.name = name
    
    # 3. Apply Voxel Remesh to fuse into a seamless organic watertight shape
    set_active_and_selected(obj)
    remesh_mod = obj.modifiers.new(name="Lobe_Remesh", type='REMESH')
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = 0.035
    remesh_mod.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh_mod.name)
    
    # 4. Create a Medial Mask Vertex Group to suppress gyri convolutions on the flat medial wall
    vg = obj.vertex_groups.new(name="Medial_Mask")
    mesh = obj.data
    for vert in mesh.vertices:
        # Distance from midline (x = 0). Supress displacement near the midline.
        x = vert.co.x
        weight = 1.0 - math.exp(-12.0 * abs(x))
        vg.add([vert.index], weight, 'REPLACE')
        
    # 5. Create procedural textures for high-fidelity convolutions (Gyri & Sulci)
    # Broad meandering gyri
    broad_noise = bpy.data.textures.new(name=f"{name}_Broad_Gyri", type='DISTORTED_NOISE')
    broad_noise.noise_distortion = 'CELL_NOISE'
    broad_noise.distortion = 2.4
    broad_noise.noise_scale = 0.08
    
    # Fine vascular and structural convolutions
    fine_noise = bpy.data.textures.new(name=f"{name}_Fine_Convolutions", type='DISTORTED_NOISE')
    fine_noise.noise_distortion = 'VORONOI_F1'
    fine_noise.distortion = 1.2
    fine_noise.noise_scale = 0.03
    
    # 6. Apply multi-stage displacement modifiers for natural biological complexity
    set_active_and_selected(obj)
    
    # Displace 1: Winding Gyri
    disp_broad = obj.modifiers.new(name="Broad_Gyri", type='DISPLACE')
    disp_broad.texture = broad_noise
    disp_broad.texture_coords = 'LOCAL'
    disp_broad.strength = 0.055
    disp_broad.vertex_group = "Medial_Mask"
    bpy.ops.object.modifier_apply(modifier=disp_broad.name)
    
    # Displace 2: Fine organic micro-textures
    disp_fine = obj.modifiers.new(name="Fine_Details", type='DISPLACE')
    disp_fine.texture = fine_noise
    disp_fine.texture_coords = 'LOCAL'
    disp_fine.strength = 0.012
    disp_fine.vertex_group = "Medial_Mask"
    bpy.ops.object.modifier_apply(modifier=disp_fine.name)
    
    # 7. Add Smooth modifier to round out folds organically
    smooth = obj.modifiers.new(name="Organic_Smooth", type='SMOOTH')
    smooth.factor = 0.75
    smooth.iterations = 3
    bpy.ops.object.modifier_apply(modifier=smooth.name)
    
    # 7b. Optimize Mesh Density to drastically reduce GLB size (Decimate to 5% faces)
    optimize_mesh_density(obj, ratio=0.05)
    
    # 8. Shift hemisphere laterally to correct anatomical position
    set_active_and_selected(obj)
    lateral_shift = -0.06 if is_left else 0.06
    for vert in mesh.vertices:
        vert.co.x += lateral_shift
        
    mesh.update()
    bpy.ops.object.shade_smooth()
    
    return obj

def generate_cerebellum():
    """Generates the cerebellum with dense horizontal ribbed folds (folia) using Wave-displacement and decimation."""
    # 1. Create vermis and lateral lobes primitives
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=(0, -0.65, -0.45))
    vermis = bpy.context.active_object
    vermis.name = "Vermis"
    vermis.scale = (0.28, 0.42, 0.42)
    
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=(-0.25, -0.65, -0.45))
    lobe_l = bpy.context.active_object
    lobe_l.name = "Lobe_Left"
    lobe_l.scale = (0.48, 0.52, 0.45)
    
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=(0.25, -0.65, -0.45))
    lobe_r = bpy.context.active_object
    lobe_r.name = "Lobe_Right"
    lobe_r.scale = (0.48, 0.52, 0.45)
    
    # 2. Join into a single cerebellum structure
    bpy.ops.object.select_all(action='DESELECT')
    vermis.select_set(True)
    lobe_l.select_set(True)
    lobe_r.select_set(True)
    bpy.context.view_layer.objects.active = vermis
    bpy.ops.object.join()
    
    obj = bpy.context.active_object
    obj.name = "Cerebellum"
    
    # 3. Voxel remesh to merge lobes smoothly
    set_active_and_selected(obj)
    remesh_mod = obj.modifiers.new(name="Cerebellum_Remesh", type='REMESH')
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = 0.024
    remesh_mod.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh_mod.name)
    
    # 4. Create an Anterior Mask Vertex Group to suppress folia where the cerebellum meets the brainstem
    vg = obj.vertex_groups.new(name="Anterior_Mask")
    mesh = obj.data
    for vert in mesh.vertices:
        y = vert.co.y
        # Cerebellum center is at y=-0.65. Suppression fades out towards the front (pons)
        weight = 1.0 - math.exp(-6.0 * (abs(y - (-0.65)) if y > -0.65 else 0.0))
        vg.add([vert.index], weight, 'REPLACE')
        
    # 5. Create a WOOD texture in bands mode to represent the fine cerebellar horizontal folia
    folia_noise = bpy.data.textures.new(name="Cerebellum_Folia", type='WOOD')
    folia_noise.wood_type = 'BANDS'
    folia_noise.noise_scale = 0.035
    folia_noise.contrast = 1.2
    
    # 6. Set up a rotated Empty to map the vertical wood bands into perfectly horizontal parallel folds
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, -0.65, -0.45))
    empty_coords = bpy.context.active_object
    empty_coords.name = "Cerebellum_Tex_Coords"
    empty_coords.rotation_euler = (0, math.pi / 2, 0)  # Rotates the local mapping 90 degrees around Y
    
    # 7. Apply folia displacement modifier
    set_active_and_selected(obj)
    disp_folia = obj.modifiers.new(name="Folia_Bands", type='DISPLACE')
    disp_folia.texture = folia_noise
    disp_folia.texture_coords = 'OBJECT'
    disp_folia.texture_coords_object = empty_coords
    disp_folia.strength = 0.016
    disp_folia.vertex_group = "Anterior_Mask"
    bpy.ops.object.modifier_apply(modifier=disp_folia.name)
    
    # 8. Smooth folds organically
    smooth = obj.modifiers.new(name="Cerebellum_Smooth", type='SMOOTH')
    smooth.factor = 0.65
    smooth.iterations = 2
    bpy.ops.object.modifier_apply(modifier=smooth.name)
    
    # Optimize vertex density to 7% faces for high-performance WebGL
    optimize_mesh_density(obj, ratio=0.07)
    
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_corpus_callosum():
    """Generates the central white matter bridge (Corpus Callosum) along the sagittal midline."""
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=64,
        ring_count=32,
        radius=0.4,
        location=(0, 0, 0.1)
    )
    obj = bpy.context.active_object
    obj.name = "Corpus_Callosum"
    mesh = obj.data
    
    for vert in mesh.vertices:
        # Variable thickness along the C-arch (thick at poles, thin in body)
        genu_thick = 1.0 + 0.35 * max(0.0, vert.co.y)**2
        splenium_thick = 1.0 + 0.55 * max(0.0, -vert.co.y)**2
        thick = genu_thick * splenium_thick
        
        # Flatten laterally
        vert.co.x *= 0.15 * thick
        
        # Arch mathematical deformation
        y = vert.co.y * 1.5 - 0.1
        z = vert.co.z * 0.85 * thick + 0.15
        
        bend = -0.35 * (y**2) + 0.15
        z += bend
        
        vert.co.y = y
        vert.co.z = z
        
    mesh.update()
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_thalamus(is_left=True):
    """Generates one of the egg-shaped central relay hubs (Thalamus) with prominent LGN/MGN protrusions."""
    lateral_shift = -0.16 if is_left else 0.16
    
    # 1. Create main egg-shaped thalamic body
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=(lateral_shift, -0.15, -0.08))
    main_body = bpy.context.active_object
    main_body.name = "Thalamus_Body"
    main_body.scale = (0.18, 0.28, 0.18)
    
    # 2. Create sensory nuclei protrusions on the posterior-lateral-inferior aspect
    # LGN (Lateral Geniculate Nucleus)
    lgn_loc = (lateral_shift * 1.25, -0.28, -0.18)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.038, location=lgn_loc)
    lgn = bpy.context.active_object
    lgn.name = "LGN"
    
    # MGN (Medial Geniculate Nucleus)
    mgn_loc = (lateral_shift * 1.05, -0.31, -0.19)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.032, location=mgn_loc)
    mgn = bpy.context.active_object
    mgn.name = "MGN"
    
    # 3. Join protrusions with the main body
    bpy.ops.object.select_all(action='DESELECT')
    main_body.select_set(True)
    lgn.select_set(True)
    mgn.select_set(True)
    bpy.context.view_layer.objects.active = main_body
    bpy.ops.object.join()
    
    obj = bpy.context.active_object
    obj.name = f"Thalamus_{'Left' if is_left else 'Right'}"
    
    # 4. Voxel Remesh to blend the protrusions flawlessly into the main structure
    set_active_and_selected(obj)
    remesh_mod = obj.modifiers.new(name="Thalamus_Remesh", type='REMESH')
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = 0.012
    remesh_mod.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh_mod.name)
    
    # Organic smoothing
    smooth = obj.modifiers.new(name="Thalamus_Smooth", type='SMOOTH')
    smooth.factor = 0.65
    smooth.iterations = 2
    bpy.ops.object.modifier_apply(modifier=smooth.name)
    
    # Decimate density to 6% faces
    optimize_mesh_density(obj, ratio=0.06)
    
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_interthalamic_adhesion():
    """Generates the Massa Intermedia (Interthalamic Adhesion) connecting left and right thalamus."""
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=32,
        radius=0.042,
        depth=0.32,
        location=(0, -0.15, -0.08),
        rotation=(0, math.pi/2, 0)
    )
    obj = bpy.context.active_object
    obj.name = "Interthalamic_Adhesion"
    mesh = obj.data
    
    for vert in mesh.vertices:
        # local z is along cylinder length
        t = vert.co.z
        taper = 1.0 - 0.25 * math.exp(-35.0 * t**2)
        vert.co.x *= taper
        vert.co.y *= taper
        
    mesh.update()
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_hypothalamus():
    """Generates the hypothalamus below the thalamus, featuring prominent Mammillary Bodies."""
    # 1. Base Hypothalamus structure
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=(0, -0.06, -0.25))
    base = bpy.context.active_object
    base.name = "Hypo_Base"
    base.scale = (0.12, 0.14, 0.1)
    
    # Mammillary body twin posterior-inferior protrusions
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.034, location=(-0.038, -0.14, -0.25))
    mb_l = bpy.context.active_object
    mb_l.name = "MB_L"
    
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.034, location=(0.038, -0.14, -0.25))
    mb_r = bpy.context.active_object
    mb_r.name = "MB_R"
    
    # Join into a single cohesive structure
    bpy.ops.object.select_all(action='DESELECT')
    base.select_set(True)
    mb_l.select_set(True)
    mb_r.select_set(True)
    bpy.context.view_layer.objects.active = base
    bpy.ops.object.join()
    
    obj = bpy.context.active_object
    obj.name = "Hypothalamus"
    
    # Voxel remesh to organically unify the mammillary bodies and the base
    set_active_and_selected(obj)
    remesh_mod = obj.modifiers.new(name="Hypo_Remesh", type='REMESH')
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = 0.01
    remesh_mod.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh_mod.name)
    
    # General organic shaping: taper front and push down
    mesh = obj.data
    for vert in mesh.vertices:
        y = vert.co.y
        taper = 1.0 - 0.25 * max(0.0, y - (-0.06))
        vert.co.x *= taper
        vert.co.z *= taper
        if y > -0.06:
            vert.co.z -= 0.08 * (y - (-0.06))
            
    mesh.update()
    
    # Decimate density to 6% faces
    optimize_mesh_density(obj, ratio=0.06)
    
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_infundibulum_stalk():
    """Generates the infundibular stalk connecting the hypothalamus to the pituitary gland."""
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=24,
        radius=0.018,
        depth=0.12,
        location=(0, 0.03, -0.42)
    )
    obj = bpy.context.active_object
    obj.name = "Infundibulum_Stalk"
    
    obj.rotation_euler[0] = math.radians(45.0) # Slope down-forward
    mesh = obj.data
    for vert in mesh.vertices:
        # Taper downwards (local -Z)
        taper = 1.0 - 0.35 * vert.co.z
        vert.co.x *= taper
        vert.co.y *= taper
        
    mesh.update()
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_pituitary_gland():
    """Generates a detailed, bean-like bi-lobed pituitary gland with distinct anterior and posterior lobes."""
    # 1. Create anterior and posterior lobe spheres
    # Anterior lobe (larger, front)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=10, radius=1.0, location=(0, 0.055, -0.45))
    ant_lobe = bpy.context.active_object
    ant_lobe.name = "Ant_Lobe"
    ant_lobe.scale = (0.075, 0.045, 0.045)
    
    # Posterior lobe (smaller, back)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=10, radius=1.0, location=(0, 0.025, -0.455))
    post_lobe = bpy.context.active_object
    post_lobe.name = "Post_Lobe"
    post_lobe.scale = (0.055, 0.038, 0.038)
    
    # 2. Join the lobes
    bpy.ops.object.select_all(action='DESELECT')
    ant_lobe.select_set(True)
    post_lobe.select_set(True)
    bpy.context.view_layer.objects.active = ant_lobe
    bpy.ops.object.join()
    
    obj = bpy.context.active_object
    obj.name = "Pituitary_Gland"
    
    # 3. Voxel Remesh to merge lobes while retaining a subtle anatomical dividing cleft
    set_active_and_selected(obj)
    remesh_mod = obj.modifiers.new(name="Pituitary_Remesh", type='REMESH')
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = 0.008
    remesh_mod.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh_mod.name)
    
    # 4. Shape into an organic kidney-bean
    mesh = obj.data
    for vert in mesh.vertices:
        vert.co.x *= 1.35
        # Cleft indentation in the middle
        y = vert.co.y
        groove = 1.0 - 0.12 * math.exp(-150.0 * (y - 0.045)**2)
        vert.co.x *= groove
        vert.co.z *= groove
        
    mesh.update()
    
    # Decimate density to 6% faces
    optimize_mesh_density(obj, ratio=0.06)
    
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_pineal_gland():
    """Generates a pinecone-shaped pineal gland with realistic Fibonacci spiral scale displacement."""
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=32,
        ring_count=16,
        radius=0.05,
        location=(0, -0.38, -0.15)
    )
    obj = bpy.context.active_object
    obj.name = "Pineal_Gland"
    mesh = obj.data
    
    for vert in mesh.vertices:
        # Elongate along Y-axis and taper posteriorly
        taper = 1.0 - 0.45 * (vert.co.y + 0.05)
        vert.co.y *= 1.45
        
        # Fibonacci spiral / phyllotaxis scale displacement
        angle = math.atan2(vert.co.x, vert.co.z)
        # Using spiral frequencies to mimic overlapping scales
        spiral = math.sin(6.0 * angle + 24.0 * vert.co.y) * math.cos(3.0 * angle - 18.0 * vert.co.y)
        disp = 0.075 * max(0.0, spiral)
        
        vert.co.x *= taper * (1.0 + disp)
        vert.co.z *= taper * (1.0 + disp)
        
    mesh.update()
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_pineal_stalks():
    """Generates the V-shaped Habenular Stalks anchoring the pineal gland forward into the epithalamus."""
    curve_data = bpy.data.curves.new('Pineal_Stalks_Curve', type='CURVE')
    curve_data.dimensions = '3D'
    curve_data.bevel_depth = 0.008
    curve_data.bevel_resolution = 3
    
    # Left habenula: from pineal (0, -0.38, -0.15) to posterior thalamus (-0.05, -0.31, -0.13)
    spline_l = curve_data.splines.new('BEZIER')
    spline_l.bezier_points.add(2)
    pts_l = [
        (0.0, -0.38, -0.15),
        (-0.025, -0.34, -0.14),
        (-0.05, -0.31, -0.13)
    ]
    for idx, pt in enumerate(pts_l):
        bp = spline_l.bezier_points[idx]
        bp.co = pt
        bp.handle_left_type = 'AUTO'
        bp.handle_right_type = 'AUTO'
        
    # Right habenula: symmetric
    spline_r = curve_data.splines.new('BEZIER')
    spline_r.bezier_points.add(2)
    pts_r = [
        (0.0, -0.38, -0.15),
        (0.025, -0.34, -0.14),
        (0.05, -0.31, -0.13)
    ]
    for idx, pt in enumerate(pts_r):
        bp = spline_r.bezier_points[idx]
        bp.co = pt
        bp.handle_left_type = 'AUTO'
        bp.handle_right_type = 'AUTO'
        
    obj = bpy.data.objects.new("Pineal_Stalks", curve_data)
    bpy.context.collection.objects.link(obj)
    
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target='MESH')
    obj = bpy.context.active_object
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_hippocampus(is_left=True):
    """Generates an anatomically correct hippocampus curve with transverse pes digitations at the head."""
    lat_sign = -1 if is_left else 1
    
    # 1. Generate elegant seahorse-shaped pathway using a Bezier curve
    curve_data = bpy.data.curves.new('Hippo_Curve', type='CURVE')
    curve_data.dimensions = '3D'
    curve_data.bevel_depth = 0.038
    curve_data.bevel_resolution = 4
    
    spline = curve_data.splines.new('BEZIER')
    spline.bezier_points.add(2)  # Total 3 control points
    
    # Anatomical trajectory: Head curves anteriorly, body sweeps back and arches superiorly
    pts = [
        (lat_sign * 0.28, -0.05, -0.38),   # Head (ventral-anterior)
        (lat_sign * 0.30, -0.18, -0.34),   # Body (sweeps postero-lateral)
        (lat_sign * 0.26, -0.30, -0.25)    # Tail (sweeps postero-superior)
    ]
    radii = [1.8, 1.3, 0.8]  # Taper radius (Head is thickest, tail is thinnest)
    
    for idx, pt in enumerate(pts):
        bp = spline.bezier_points[idx]
        bp.co = pt
        bp.radius = radii[idx]
        bp.handle_left_type = 'AUTO'
        bp.handle_right_type = 'AUTO'
        
    obj_curve = bpy.data.objects.new("Hippocampus_Curve", curve_data)
    bpy.context.collection.objects.link(obj_curve)
    
    # 2. Select and convert curve to mesh
    bpy.ops.object.select_all(action='DESELECT')
    obj_curve.select_set(True)
    bpy.context.view_layer.objects.active = obj_curve
    bpy.ops.object.convert(target='MESH')
    
    obj = bpy.context.active_object
    obj.name = f"Hippocampus_{'Left' if is_left else 'Right'}"
    
    # 3. Voxel remesh to get uniform density for detailed biological texturing
    set_active_and_selected(obj)
    remesh_mod = obj.modifiers.new(name="Hippo_Remesh", type='REMESH')
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = 0.012
    remesh_mod.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh_mod.name)
    
    # 4. Create a Vertex Group to mask the digitations (only on the anterior head segment, y > -0.15)
    vg = obj.vertex_groups.new(name="Pes_Digitations_Mask")
    mesh = obj.data
    for vert in mesh.vertices:
        y = vert.co.y
        # Weight is 1.0 on the anterior head, transitioning to 0.0 in the body
        weight = 1.0 if y > -0.12 else (max(0.0, (y - (-0.2)) / 0.08) if y > -0.2 else 0.0)
        vg.add([vert.index], weight, 'REPLACE')
        
    # 5. Displace using a fine parallel WOOD bands texture to represent transverse digital folds
    digit_noise = bpy.data.textures.new(name=f"{obj.name}_Pes_Waves", type='WOOD')
    digit_noise.wood_type = 'BANDS'
    digit_noise.noise_scale = 0.024
    
    # Rotation coordinate empty for the displacement direction
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(lat_sign * 0.28, -0.05, -0.38))
    empty_coords = bpy.context.active_object
    empty_coords.name = f"{obj.name}_Tex_Coords"
    empty_coords.rotation_euler = (0, 0, 0)
    
    set_active_and_selected(obj)
    disp = obj.modifiers.new(name="Pes_Digitations", type='DISPLACE')
    disp.texture = digit_noise
    disp.texture_coords = 'OBJECT'
    disp.texture_coords_object = empty_coords
    disp.strength = 0.008
    disp.vertex_group = "Pes_Digitations_Mask"
    bpy.ops.object.modifier_apply(modifier=disp.name)
    
    # Smooth detailing
    smooth = obj.modifiers.new(name="Hippo_Smooth", type='SMOOTH')
    smooth.factor = 0.65
    smooth.iterations = 2
    bpy.ops.object.modifier_apply(modifier=smooth.name)
    
    # Decimate density to 6% faces
    optimize_mesh_density(obj, ratio=0.06)
    
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_fornix():
    """Generates the elegant double-stranded arching white matter tract (Fornix)."""
    curve_data = bpy.data.curves.new('Fornix_Curve_Data', type='CURVE')
    curve_data.dimensions = '3D'
    curve_data.bevel_depth = 0.018
    curve_data.bevel_resolution = 4
    
    # We create two symmetric strands (Left & Right) curving from posterior hippocampi up, merging midline, and descending anteriorly
    for is_left in [True, False]:
        lat_sign = -1 if is_left else 1
        spline = curve_data.splines.new('BEZIER')
        spline.bezier_points.add(4)  # 5 control points
        
        pts = [
            (lat_sign * 0.22, -0.28, -0.22),  # Start at posterior Hippocampus
            (lat_sign * 0.12, -0.24, -0.06),  # Sweep posterolaterally
            (lat_sign * 0.015, -0.12, 0.08),  # Merge at midline under Corpus Callosum
            (lat_sign * 0.02, 0.0, -0.12),    # Columns split anteriorly
            (lat_sign * 0.03, -0.06, -0.25)   # Descend into Mammillary Bodies
        ]
        radii = [0.6, 0.8, 1.0, 0.8, 0.6]  # Thick in the middle body, thin at terminals
        
        for idx, pt in enumerate(pts):
            bp = spline.bezier_points[idx]
            bp.co = pt
            bp.radius = radii[idx]
            bp.handle_left_type = 'AUTO'
            bp.handle_right_type = 'AUTO'
            
    obj_curve = bpy.data.objects.new("Fornix", curve_data)
    bpy.context.collection.objects.link(obj_curve)
    
    # Convert to mesh for visual consistency
    bpy.ops.object.select_all(action='DESELECT')
    obj_curve.select_set(True)
    bpy.context.view_layer.objects.active = obj_curve
    bpy.ops.object.convert(target='MESH')
    
    obj = bpy.context.active_object
    
    # Subtle Voxel Remesh to fuse midline strands cleanly
    set_active_and_selected(obj)
    remesh_mod = obj.modifiers.new(name="Fornix_Remesh", type='REMESH')
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = 0.01
    remesh_mod.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh_mod.name)
    
    # Polish
    smooth = obj.modifiers.new(name="Fornix_Smooth", type='SMOOTH')
    smooth.factor = 0.7
    smooth.iterations = 2
    bpy.ops.object.modifier_apply(modifier=smooth.name)
    
    # Decimate density to 6% faces
    optimize_mesh_density(obj, ratio=0.06)
    
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_amygdala(is_left=True):
    """Generates an organic almond-shaped emotional salience center with low-frequency nuclear divisions."""
    lat_shift = -0.34 if is_left else 0.34
    
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=24,
        ring_count=12,
        radius=0.08,
        location=(lat_shift, 0.0, -0.36)
    )
    obj = bpy.context.active_object
    obj.name = f"Amygdala_{'Left' if is_left else 'Right'}"
    mesh = obj.data
    
    # Almond shaping (flattened laterally, pointed anteriorly)
    for vert in mesh.vertices:
        vert.co.x *= 0.65
        vert.co.z *= 0.75
        y = vert.co.y
        if y > 0:
            taper = 1.0 - 0.55 * y
            vert.co.x *= taper
            vert.co.z *= taper
        vert.co.y *= 1.35
        
        # Micro nuclei bumps (low strength, high frequency noise)
        wiggles = 1.0 + 0.04 * (math.sin(20.0 * vert.co.x) * math.cos(20.0 * vert.co.y) * math.sin(20.0 * vert.co.z))
        vert.co.x *= wiggles
        vert.co.y *= wiggles
        vert.co.z *= wiggles
        
    mesh.update()
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_caudate(is_left=True):
    """Generates the C-shaped caudate nucleus curving flawlessly around the thalamus using a beveled curve."""
    lat_sign = -1 if is_left else 1
    
    # 1. Create a C-shaped Bezier curve with tapering thickness (thick head, thin tail)
    curve_data = bpy.data.curves.new('Caudate_Curve', type='CURVE')
    curve_data.dimensions = '3D'
    curve_data.bevel_depth = 0.045
    curve_data.bevel_resolution = 4
    
    spline = curve_data.splines.new('BEZIER')
    spline.bezier_points.add(4)  # Total 5 points
    
    # Path coordinates sweeping over and around thalamus
    pts = [
        (lat_sign * 0.14, -0.06, -0.18),  # Head (ventral-anterior)
        (lat_sign * 0.16, 0.06, 0.06),   # Body (curves antero-superior)
        (lat_sign * 0.20, -0.14, 0.20),   # Body (sweeps superior-posterior)
        (lat_sign * 0.24, -0.32, 0.04),   # Tail (sweeps postero-inferior)
        (lat_sign * 0.26, -0.26, -0.16)   # Tail end (curves antero-inferior into temporal lobe)
    ]
    # Radius parameters for smooth taper
    radii = [2.0, 1.6, 1.2, 0.6, 0.35]
    
    for idx, pt in enumerate(pts):
        bp = spline.bezier_points[idx]
        bp.co = pt
        bp.radius = radii[idx]
        bp.handle_left_type = 'AUTO'
        bp.handle_right_type = 'AUTO'
        
    obj_curve = bpy.data.objects.new("Caudate_Curve_Obj", curve_data)
    bpy.context.collection.objects.link(obj_curve)
    
    # 2. Select and convert to mesh
    bpy.ops.object.select_all(action='DESELECT')
    obj_curve.select_set(True)
    bpy.context.view_layer.objects.active = obj_curve
    bpy.ops.object.convert(target='MESH')
    
    obj = bpy.context.active_object
    obj.name = f"Caudate_{'Left' if is_left else 'Right'}"
    
    # 3. Voxel remesh to solidify and smooth the taper perfectly
    set_active_and_selected(obj)
    remesh_mod = obj.modifiers.new(name="Caudate_Remesh", type='REMESH')
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = 0.01
    remesh_mod.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh_mod.name)
    
    # Polish
    smooth = obj.modifiers.new(name="Caudate_Smooth", type='SMOOTH')
    smooth.factor = 0.7
    smooth.iterations = 2
    bpy.ops.object.modifier_apply(modifier=smooth.name)
    
    # Decimate density to 6% faces
    optimize_mesh_density(obj, ratio=0.06)
    
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_putamen(is_left=True):
    """Generates the shell-shaped putamen lateral to the globus pallidus."""
    lat_sign = -1 if is_left else 1
    
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=32,
        ring_count=16,
        radius=1.0,
        location=(lat_sign * 0.26, -0.12, 0.04)
    )
    obj = bpy.context.active_object
    obj.name = f"Putamen_{'Left' if is_left else 'Right'}"
    
    # Lens/Shell shaping
    obj.scale = (0.12, 0.22, 0.18)
    
    # Solidify shape using remesh
    set_active_and_selected(obj)
    remesh_mod = obj.modifiers.new(name="Putamen_Remesh", type='REMESH')
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = 0.012
    remesh_mod.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh_mod.name)
    
    # Decimate density to 6% faces
    optimize_mesh_density(obj, ratio=0.06)
    
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_globus_pallidus(is_left=True):
    """Generates the wedge-shaped globus pallidus medial to the putamen."""
    lat_sign = -1 if is_left else 1
    
    # Wedge shaped, modelled medial to the putamen
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=24,
        ring_count=12,
        radius=1.0,
        location=(lat_sign * 0.20, -0.12, 0.02)
    )
    obj = bpy.context.active_object
    obj.name = f"Globus_Pallidus_{'Left' if is_left else 'Right'}"
    
    # Scale to nest inside putamen shell
    obj.scale = (0.08, 0.16, 0.12)
    mesh = obj.data
    
    # Wedge-shaping: compress the medial side (smaller x)
    for vert in mesh.vertices:
        # In local space, compress towards the center
        vert.co.x *= 0.85
        
    mesh.update()
    
    # Remesh to fuse cleanly
    set_active_and_selected(obj)
    remesh_mod = obj.modifiers.new(name="GP_Remesh", type='REMESH')
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = 0.01
    remesh_mod.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh_mod.name)
    
    # Decimate density to 6% faces
    optimize_mesh_density(obj, ratio=0.06)
    
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_nucleus_accumbens(is_left=True):
    """Generates the nucleus accumbens at the ventral striatum bridging caudate and putamen."""
    lat_sign = -1 if is_left else 1
    
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=24,
        ring_count=12,
        radius=1.0,
        location=(lat_sign * 0.18, -0.04, -0.22)
    )
    obj = bpy.context.active_object
    obj.name = f"Nucleus_Accumbens_{'Left' if is_left else 'Right'}"
    obj.scale = (0.07, 0.08, 0.07)
    
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_substantia_nigra(is_left=True):
    """Generates the slanted sheet of cells representing substantia nigra in the midbrain."""
    lat_sign = -1 if is_left else 1
    
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=24,
        ring_count=12,
        radius=0.09,
        location=(0, 0, 0)
    )
    obj = bpy.context.active_object
    obj.name = f"Substantia_Nigra_{'Left' if is_left else 'Right'}"
    mesh = obj.data
    
    lateral_shift = lat_sign * 0.13
    
    for vert in mesh.vertices:
        ox = vert.co.x
        oy = vert.co.z
        oz = vert.co.y
        
        vert.co.x = ox * 0.85 + lateral_shift
        vert.co.y = oz * 1.1 - 0.33 + 0.12 * ox
        vert.co.z = oy * 0.4 - 0.31
        
    mesh.update()
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_vta():
    """Generates the midline, bi-lobed Ventral Tegmental Area (VTA) in the midbrain."""
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=24,
        ring_count=12,
        radius=0.08,
        location=(0, 0, 0)
    )
    obj = bpy.context.active_object
    obj.name = "VTA"
    mesh = obj.data
    
    for vert in mesh.vertices:
        ox = vert.co.x
        oy = vert.co.z
        oz = vert.co.y
        
        double_lobe = 1.0 + 0.25 * abs(ox)
        vert.co.x = ox * 1.2
        vert.co.y = oz * double_lobe - 0.3
        vert.co.z = oy * 0.9 - 0.24
        
    mesh.update()
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_midbrain():
    """Generates the midbrain (mesencephalon) featuring cerebral peduncles and corpora quadrigemina bulges."""
    # 1. Base midbrain cylinder
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.15, depth=0.18, location=(0, -0.18, -0.38))
    base = bpy.context.active_object
    base.name = "Midbrain_Base"
    
    # 2. Anterior Cerebral Peduncles (twin vertical columns)
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.045, depth=0.18, location=(-0.05, -0.11, -0.38))
    ped_l = bpy.context.active_object
    ped_l.name = "Ped_L"
    ped_l.rotation_euler = (0, math.radians(10.0), 0)
    
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.045, depth=0.18, location=(0.05, -0.11, -0.38))
    ped_r = bpy.context.active_object
    ped_r.name = "Ped_R"
    ped_r.rotation_euler = (0, math.radians(-10.0), 0)
    
    # 3. Posterior Corpora Quadrigemina twin collicular bulges
    # Superior Colliculi (larger, upper)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.032, location=(-0.045, -0.28, -0.32))
    sc_l = bpy.context.active_object
    sc_l.name = "SC_L"
    
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.032, location=(0.045, -0.28, -0.32))
    sc_r = bpy.context.active_object
    sc_r.name = "SC_R"
    
    # Inferior Colliculi (smaller, lower)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.026, location=(-0.04, -0.29, -0.38))
    ic_l = bpy.context.active_object
    ic_l.name = "IC_L"
    
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.026, location=(0.04, -0.29, -0.38))
    ic_r = bpy.context.active_object
    ic_r.name = "IC_R"
    
    # 4. Join structures
    bpy.ops.object.select_all(action='DESELECT')
    base.select_set(True)
    ped_l.select_set(True)
    ped_r.select_set(True)
    sc_l.select_set(True)
    sc_r.select_set(True)
    ic_l.select_set(True)
    ic_r.select_set(True)
    bpy.context.view_layer.objects.active = base
    bpy.ops.object.join()
    
    obj = bpy.context.active_object
    obj.name = "Midbrain"
    
    # 5. Voxel remesh to organically blend peduncles and colliculi bulges
    set_active_and_selected(obj)
    remesh_mod = obj.modifiers.new(name="Midbrain_Remesh", type='REMESH')
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = 0.012
    remesh_mod.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh_mod.name)
    
    # Interpeduncular fossa groove on midline anterior (x=0, y > -0.14)
    mesh = obj.data
    for vert in mesh.vertices:
        x = vert.co.x
        y = vert.co.y
        if y > -0.15 and abs(x) < 0.04:
            # Carve a midline vertical groove
            groove = 0.014 * (1.0 - abs(x) / 0.04)
            vert.co.y -= groove
            
    mesh.update()
    
    # Decimate density to 6% faces
    optimize_mesh_density(obj, ratio=0.06)
    
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_pons():
    """Generates the pons featuring transverse pontine fiber striations and a central basilar sulcus."""
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=48,
        ring_count=24,
        radius=1.0,
        location=(0, -0.12, -0.58)
    )
    obj = bpy.context.active_object
    obj.name = "Pons"
    obj.scale = (0.24, 0.18, 0.18)
    
    # Voxel remesh to freeze scale and get uniform vertices for detailed horizontal fibers
    set_active_and_selected(obj)
    remesh_mod = obj.modifiers.new(name="Pons_Remesh", type='REMESH')
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = 0.012
    remesh_mod.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh_mod.name)
    
    mesh = obj.data
    
    # 1. Create a vertex group to mask displacement on the anterior face, leaving the posterior face smooth
    vg = obj.vertex_groups.new(name="Pontine_Fibers_Mask")
    for vert in mesh.vertices:
        x = vert.co.x
        y = vert.co.y
        # Front is y > -0.12. Basilar groove sits vertically in the midline (x=0)
        # We suppress displacement at the vertical midline x=0 to carve the basilar sulcus
        weight = 0.0
        if y > -0.15:
            weight = 1.0 - math.exp(-12.0 * abs(x))
        vg.add([vert.index], weight, 'REPLACE')
        
    # 2. Add horizontal transverse fibers using a high-frequency wood bands texture
    pons_noise = bpy.data.textures.new(name="Transverse_Pontine_Fibers", type='WOOD')
    pons_noise.wood_type = 'BANDS'
    pons_noise.noise_scale = 0.015
    pons_noise.contrast = 1.2
    
    # Empty for coordinate rotation to keep bands horizontal
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, -0.12, -0.58))
    empty_coords = bpy.context.active_object
    empty_coords.name = "Pons_Tex_Coords"
    empty_coords.rotation_euler = (0, math.pi / 2, 0)
    
    set_active_and_selected(obj)
    disp = obj.modifiers.new(name="Pontine_Striations", type='DISPLACE')
    disp.texture = pons_noise
    disp.texture_coords = 'OBJECT'
    disp.texture_coords_object = empty_coords
    disp.strength = 0.006
    disp.vertex_group = "Pontine_Fibers_Mask"
    bpy.ops.object.modifier_apply(modifier=disp.name)
    
    # 3. Basilar Sulcus vertical midline groove (carved via post-displacement coordinate warp)
    for vert in mesh.vertices:
        x = vert.co.x
        y = vert.co.y
        if y > -0.15 and abs(x) < 0.038:
            pinch = 1.0 - 0.12 * math.cos(math.pi * x / (2.0 * 0.038))
            vert.co.y *= pinch
            
    mesh.update()
    
    # Decimate density to 6% faces
    optimize_mesh_density(obj, ratio=0.06)
    
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_medulla_oblongata():
    """Generates the medulla oblongata featuring Pyramids, Anterior Median Fissure, and lateral Olives."""
    # 1. Base medulla cylinder
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.11, depth=0.28, location=(0, -0.22, -0.84))
    base = bpy.context.active_object
    base.name = "Medulla_Base"
    base.scale = (1.0, 0.85, 1.0)
    
    # 2. Pyramids (twin longitudinal columns flanking the midline)
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.032, depth=0.28, location=(-0.03, -0.13, -0.84))
    pyr_l = bpy.context.active_object
    pyr_l.name = "Pyr_L"
    
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.032, depth=0.28, location=(0.03, -0.13, -0.84))
    pyr_r = bpy.context.active_object
    pyr_r.name = "Pyr_R"
    
    # 3. Olives (lateral oval protrusions)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=1.0, location=(-0.08, -0.15, -0.82))
    olive_l = bpy.context.active_object
    olive_l.name = "Olive_L"
    olive_l.scale = (0.035, 0.05, 0.028)
    
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=1.0, location=(0.08, -0.15, -0.82))
    olive_r = bpy.context.active_object
    olive_r.name = "Olive_R"
    olive_r.scale = (0.035, 0.05, 0.028)
    
    # 4. Join and Voxel Remesh
    bpy.ops.object.select_all(action='DESELECT')
    base.select_set(True)
    pyr_l.select_set(True)
    pyr_r.select_set(True)
    olive_l.select_set(True)
    olive_r.select_set(True)
    bpy.context.view_layer.objects.active = base
    bpy.ops.object.join()
    
    obj = bpy.context.active_object
    obj.name = "Medulla_Oblongata"
    
    set_active_and_selected(obj)
    remesh_mod = obj.modifiers.new(name="Medulla_Remesh", type='REMESH')
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = 0.012
    remesh_mod.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh_mod.name)
    
    # Decimate density to 6% faces
    optimize_mesh_density(obj, ratio=0.06)
    
    # 5. Apply tapering and Anterior Median Fissure (narrow vertical groove at x=0)
    set_active_and_selected(obj)
    mesh = obj.data
    for vert in mesh.vertices:
        x = vert.co.x
        y = vert.co.y
        z = vert.co.z  # ranges from -0.98 to -0.70
        
        # Downward taper
        taper = 1.0 - 0.35 * (z - (-0.70)) / (-0.28)
        vert.co.x *= taper
        vert.co.y *= taper
        
        # Midline fissure
        if y > -0.15 and abs(x) < 0.012:
            groove = 0.012 * (1.0 - abs(x) / 0.012)
            vert.co.y -= groove
            
    mesh.update()
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_spinal_cord():
    """Generates the spinal cord with Anterior Median Fissure and Posterior Median Sulcus."""
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=32,
        radius=0.10,
        depth=0.5,
        location=(0, -0.36, -1.25)
    )
    obj = bpy.context.active_object
    obj.name = "Spinal_Cord"
    mesh = obj.data
    
    for vert in mesh.vertices:
        # 1. Anterior Median Fissure (y > 0)
        if vert.co.y > 0:
            if abs(vert.co.x) < 0.012:
                vert.co.y -= 0.01 * (1.0 - abs(vert.co.x)/0.012)
                
        # 2. Posterior Median Sulcus (y < 0)
        if vert.co.y < 0:
            if abs(vert.co.x) < 0.010:
                vert.co.y += 0.008 * (1.0 - abs(vert.co.x)/0.01)
                
        # Curve slightly backwards
        vert.co.y += 0.05 * math.sin(vert.co.z * math.pi)
        
    mesh.update()
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def generate_spinal_nerve_rootlets():
    """Generates thin, high-fidelity bilateral nerve rootlets branching off the spinal cord."""
    curve_data = bpy.data.curves.new('Rootlets_Curve', type='CURVE')
    curve_data.dimensions = '3D'
    curve_data.bevel_depth = 0.005
    curve_data.bevel_resolution = 2
    
    # Generate 6 levels of branching nerve rootlets
    for level in range(6):
        z_pos = -1.05 - level * 0.075
        
        # Left Spline
        spline_l = curve_data.splines.new('BEZIER')
        spline_l.bezier_points.add(2)
        pts_l = [
            (-0.08, -0.36, z_pos),
            (-0.16, -0.38, z_pos - 0.015),
            (-0.24, -0.42, z_pos - 0.035)
        ]
        for idx, pt in enumerate(pts_l):
            bp = spline_l.bezier_points[idx]
            bp.co = pt
            bp.handle_left_type = 'AUTO'
            bp.handle_right_type = 'AUTO'
            
        # Right Spline
        spline_r = curve_data.splines.new('BEZIER')
        spline_r.bezier_points.add(2)
        pts_r = [
            (0.08, -0.36, z_pos),
            (0.16, -0.38, z_pos - 0.015),
            (0.24, -0.42, z_pos - 0.035)
        ]
        for idx, pt in enumerate(pts_r):
            bp = spline_r.bezier_points[idx]
            bp.co = pt
            bp.handle_left_type = 'AUTO'
            bp.handle_right_type = 'AUTO'
            
    obj = bpy.data.objects.new("Spinal_Nerve_Rootlets", curve_data)
    bpy.context.collection.objects.link(obj)
    
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target='MESH')
    obj = bpy.context.active_object
    set_active_and_selected(obj)
    bpy.ops.object.shade_smooth()
    return obj

def setup_studio_lighting_and_scene():
    """Sets up a highly professional cinematic studio lighting stage to showcase the model."""
    # 1. Add Dark Reflective Ground Plate
    bpy.ops.mesh.primitive_plane_add(size=20, location=(0, 0, -2))
    ground = bpy.context.active_object
    ground.name = "Studio_Floor"
    
    ground_mat = bpy.data.materials.new(name="Studio_Floor_Mat")
    ground_mat.use_nodes = True
    nodes = ground_mat.node_tree.nodes
    nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.015, 0.015, 0.015, 1.0)
    nodes['Principled BSDF'].inputs['Roughness'].default_value = 0.18
    ground.data.materials.append(ground_mat)
    
    # 2. Add Three-Point Lighting
    # Key Light (Warm soft spotlight)
    bpy.ops.object.light_add(type='SPOT', radius=1.0, location=(3, 3, 3))
    key_light = bpy.context.active_object
    key_light.name = "Key_Light"
    key_light.data.energy = 500
    key_light.data.color = (1.0, 0.94, 0.88)
    
    constraint = key_light.constraints.new(type='TRACK_TO')
    constraint.target = bpy.data.objects.get("Cerebellum") or bpy.context.scene.objects[0]
    constraint.track_axis = 'TRACK_NEGATIVE_Z'
    constraint.up_axis = 'UP_Y'
    
    # Fill Light (Cool blue Area Light)
    bpy.ops.object.light_add(type='AREA', radius=2.0, location=(-4, -2, 1))
    fill_light = bpy.context.active_object
    fill_light.name = "Fill_Light"
    fill_light.data.energy = 150
    fill_light.data.color = (0.8, 0.88, 1.0)
    
    constraint = fill_light.constraints.new(type='TRACK_TO')
    constraint.target = bpy.data.objects.get("Cerebellum") or bpy.context.scene.objects[0]
    constraint.track_axis = 'TRACK_NEGATIVE_Z'
    constraint.up_axis = 'UP_Y'
    
    # Rim Light (Strong warm rim glow)
    bpy.ops.object.light_add(type='SPOT', radius=0.8, location=(-2, -4, 2))
    rim_light = bpy.context.active_object
    rim_light.name = "Rim_Light"
    rim_light.data.energy = 450
    rim_light.data.color = (1.0, 0.65, 0.45)
    
    constraint = rim_light.constraints.new(type='TRACK_TO')
    constraint.target = bpy.data.objects.get("Cerebellum") or bpy.context.scene.objects[0]
    constraint.track_axis = 'TRACK_NEGATIVE_Z'
    constraint.up_axis = 'UP_Y'
    
    # 3. Add Cinematic Camera
    bpy.ops.object.camera_add(location=(3.2, -3.2, 1.8))
    camera = bpy.context.active_object
    camera.name = "Cinematic_Camera"
    camera.data.lens = 55
    
    cam_constraint = camera.constraints.new(type='TRACK_TO')
    cam_constraint.target = bpy.data.objects.get("Cerebellum") or bpy.context.scene.objects[0]
    cam_constraint.track_axis = 'TRACK_NEGATIVE_Z'
    cam_constraint.up_axis = 'UP_Y'
    
    bpy.context.scene.camera = camera

def main():
    print("-----------------------------------------------------")
    print("[3D Brain Generation] Initiating high-fidelity optimized morphogenesis...")
    print("-----------------------------------------------------")
    
    clear_scene()
    
    # Create distinct organic tissue materials with realistic colors
    mats = {
        "hemisphere": create_tissue_material("Brain_Cerebrum", (0.95, 0.40, 0.65, 1.0), roughness=0.25, sss_weight=0.35),
        "cerebellum": create_tissue_material("Brain_Cerebellum", (0.70, 0.18, 0.28, 1.0), roughness=0.3, sss_weight=0.3),
        "callosum": create_tissue_material("Brain_Callosum", (0.95, 0.92, 0.88, 1.0), roughness=0.4, sss_weight=0.15),
        "thalamus": create_tissue_material("Brain_Thalamus", (0.42, 0.32, 0.85, 1.0), roughness=0.28, sss_weight=0.3),
        "limbic": create_tissue_material("Brain_Limbic", (0.10, 0.68, 0.48, 1.0), roughness=0.22, sss_weight=0.4),
        "hypothalamus": create_tissue_material("Brain_Hypothalamus", (0.95, 0.42, 0.08, 1.0), roughness=0.3, sss_weight=0.35),
        "pituitary": create_tissue_material("Brain_Pituitary", (0.08, 0.68, 0.85, 1.0), roughness=0.3, sss_weight=0.35),
        "pineal": create_tissue_material("Brain_Pineal", (0.92, 0.78, 0.12, 1.0), roughness=0.3, sss_weight=0.35),
        "stem": create_tissue_material("Brain_Stem", (0.55, 0.62, 0.70, 1.0), roughness=0.35, sss_weight=0.25),
        "caudate": create_tissue_material("Brain_Caudate", (0.98, 0.44, 0.52, 1.0), roughness=0.26, sss_weight=0.3),
        "putamen": create_tissue_material("Brain_Putamen", (0.88, 0.11, 0.28, 1.0), roughness=0.26, sss_weight=0.3),
        "globus_pallidus": create_tissue_material("Brain_Globus_Pallidus", (0.99, 0.64, 0.69, 1.0), roughness=0.28, sss_weight=0.3),
        "accumbens": create_tissue_material("Brain_Nucleus_Accumbens", (0.02, 0.84, 0.63, 1.0), roughness=0.24, sss_weight=0.35),
        "vta": create_tissue_material("Brain_VTA", (0.96, 0.62, 0.04, 1.0), roughness=0.3, sss_weight=0.3),
        "substantia_nigra": create_tissue_material("Brain_Substantia_Nigra", (0.28, 0.33, 0.41, 1.0), roughness=0.32, sss_weight=0.2)
    }
    
    structures = []
    
    # 1. Cerebrum Hemispheres
    left_hem = generate_hemisphere("Cerebrum_Left", is_left=True)
    left_hem.data.materials.append(mats["hemisphere"])
    structures.append(left_hem)
    
    right_hem = generate_hemisphere("Cerebrum_Right", is_left=False)
    right_hem.data.materials.append(mats["hemisphere"])
    structures.append(right_hem)
    
    # 2. Cerebellum
    cerebellum = generate_cerebellum()
    cerebellum.data.materials.append(mats["cerebellum"])
    structures.append(cerebellum)
    
    # 3. Corpus Callosum
    callosum = generate_corpus_callosum()
    callosum.data.materials.append(mats["callosum"])
    structures.append(callosum)
    
    # 4. Thalamus (Left & Right) and Interthalamic Adhesion
    thalamus_l = generate_thalamus(is_left=True)
    thalamus_l.data.materials.append(mats["thalamus"])
    structures.append(thalamus_l)
    
    thalamus_r = generate_thalamus(is_left=False)
    thalamus_r.data.materials.append(mats["thalamus"])
    structures.append(thalamus_r)
    
    adhesion = generate_interthalamic_adhesion()
    adhesion.data.materials.append(mats["thalamus"])
    structures.append(adhesion)
    
    # 5. Caudate, Putamen, Globus Pallidus, Nucleus Accumbens
    caudate_l = generate_caudate(is_left=True)
    caudate_l.data.materials.append(mats["caudate"])
    structures.append(caudate_l)
    
    caudate_r = generate_caudate(is_left=False)
    caudate_r.data.materials.append(mats["caudate"])
    structures.append(caudate_r)
    
    putamen_l = generate_putamen(is_left=True)
    putamen_l.data.materials.append(mats["putamen"])
    structures.append(putamen_l)
    
    putamen_r = generate_putamen(is_left=False)
    putamen_r.data.materials.append(mats["putamen"])
    structures.append(putamen_r)
    
    gp_l = generate_globus_pallidus(is_left=True)
    gp_l.data.materials.append(mats["globus_pallidus"])
    structures.append(gp_l)
    
    gp_r = generate_globus_pallidus(is_left=False)
    gp_r.data.materials.append(mats["globus_pallidus"])
    structures.append(gp_r)
    
    accumbens_l = generate_nucleus_accumbens(is_left=True)
    accumbens_l.data.materials.append(mats["accumbens"])
    structures.append(accumbens_l)
    
    accumbens_r = generate_nucleus_accumbens(is_left=False)
    accumbens_r.data.materials.append(mats["accumbens"])
    structures.append(accumbens_r)
    
    # 6. Hypothalamus and Infundibulum Stalk
    hypothalamus = generate_hypothalamus()
    hypothalamus.data.materials.append(mats["hypothalamus"])
    structures.append(hypothalamus)
    
    infundibulum = generate_infundibulum_stalk()
    infundibulum.data.materials.append(mats["hypothalamus"])
    structures.append(infundibulum)
    
    # 7. Pituitary Gland, Pineal Gland, and Pineal Stalks
    pituitary = generate_pituitary_gland()
    pituitary.data.materials.append(mats["pituitary"])
    structures.append(pituitary)
    
    pineal = generate_pineal_gland()
    pineal.data.materials.append(mats["pineal"])
    structures.append(pineal)
    
    pineal_stalks = generate_pineal_stalks()
    pineal_stalks.data.materials.append(mats["callosum"])
    structures.append(pineal_stalks)
    
    # 8. Hippocampus and Fornix
    hippo_l = generate_hippocampus(is_left=True)
    hippo_l.data.materials.append(mats["limbic"])
    structures.append(hippo_l)
    
    hippo_r = generate_hippocampus(is_left=False)
    hippo_r.data.materials.append(mats["limbic"])
    structures.append(hippo_r)
    
    fornix = generate_fornix()
    fornix.data.materials.append(mats["callosum"])
    structures.append(fornix)
    
    # 9. Amygdala
    amygdala_l = generate_amygdala(is_left=True)
    amygdala_l.data.materials.append(mats["limbic"])
    structures.append(amygdala_l)
    
    amygdala_r = generate_amygdala(is_left=False)
    amygdala_r.data.materials.append(mats["limbic"])
    structures.append(amygdala_r)
    
    # 10. Brainstem (Midbrain, Pons, Medulla, Spinal Cord, Rootlets)
    midbrain = generate_midbrain()
    midbrain.data.materials.append(mats["stem"])
    structures.append(midbrain)
    
    vta = generate_vta()
    vta.data.materials.append(mats["vta"])
    structures.append(vta)
    
    sn_l = generate_substantia_nigra(is_left=True)
    sn_l.data.materials.append(mats["substantia_nigra"])
    structures.append(sn_l)
    
    sn_r = generate_substantia_nigra(is_left=False)
    sn_r.data.materials.append(mats["substantia_nigra"])
    structures.append(sn_r)
    
    pons = generate_pons()
    pons.data.materials.append(mats["stem"])
    structures.append(pons)
    
    medulla = generate_medulla_oblongata()
    medulla.data.materials.append(mats["stem"])
    structures.append(medulla)
    
    spinal_cord = generate_spinal_cord()
    spinal_cord.data.materials.append(mats["stem"])
    structures.append(spinal_cord)
    
    rootlets = generate_spinal_nerve_rootlets()
    rootlets.data.materials.append(mats["callosum"])
    structures.append(rootlets)
    
    # Group all under a common empty parent
    bpy.ops.object.select_all(action='DESELECT')
    for s in structures:
        s.select_set(True)
        
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, -0.2, -0.2))
    brain_parent = bpy.context.active_object
    brain_parent.name = "Human_Brain"
    
    for s in structures:
        s.parent = brain_parent
        
    # Stage lights and camera
    setup_studio_lighting_and_scene()
    
    # Set Render Properties for EEVEE Next (or default EEVEE)
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE_NEXT' if hasattr(bpy.types, "RenderEngineEEVEENext") else 'BLENDER_EEVEE'
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100
    
    scene.display_settings.display_device = 'sRGB'
    scene.view_settings.view_transform = 'AgX' if hasattr(scene.view_settings, "view_transform") and 'AgX' in scene.view_settings.bl_rna.properties['view_transform'].enum_items else 'Filmic'
    scene.view_settings.look = 'High Contrast'
    
    # Save blend file
    output_blend = os.path.join(os.getcwd(), "human_brain.blend")
    bpy.ops.wm.save_as_mainfile(filepath=output_blend)
    print(f"[3D Brain Generation] Successfully saved Blender file: {output_blend}")
    
    # Render preview image
    preview_path = os.path.join(os.getcwd(), "brain_preview.png")
    scene.render.filepath = preview_path
    print(f"[3D Brain Generation] Rendering cinematic 1080p preview to: {preview_path} ...")
    bpy.ops.render.render(write_still=True)
    print("[3D Brain Generation] Rendering complete!")
    
    # Export consolidated GLB asset
    output_glb = os.path.join(os.getcwd(), "human_brain.glb")
    print(f"[3D Brain Generation] Exporting consolidated glTF/GLB asset to: {output_glb} ...")
    try:
        # Deselect all, then select only the empty brain parent and its children
        bpy.ops.object.select_all(action='DESELECT')
        brain_parent.select_set(True)
        for child in brain_parent.children:
            child.select_set(True)
            
        bpy.ops.export_scene.gltf(
            filepath=output_glb, 
            export_format='GLB',
            use_selection=True
        )
        print(f"[3D Brain Generation] Successfully exported GLB to: {output_glb}")
    except Exception as e:
        print(f"[WARNING] GLB export failed: {e}")
    print("-----------------------------------------------------")

if __name__ == "__main__":
    main()
