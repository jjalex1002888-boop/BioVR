import bpy
import math
import os
import argparse
import sys

def init_animation_settings(fps=30, end_frame=120):
    """Configures the Blender scene time parameters for smooth 30fps playback."""
    scene = bpy.context.scene
    scene.render.fps = fps
    scene.frame_start = 1
    scene.frame_end = end_frame
    scene.frame_current = 1
    
    # Ensure linear interpolation by default for smooth loops
    preferences = bpy.context.preferences
    preferences.edit.keyframe_new_interpolation_type = 'LINEAR'
    print(f"[Animation Engine] Time bounds configured: 1 to {end_frame} frames ({end_frame/fps:.2f} seconds at {fps} fps).")

def clear_animation_data():
    """Removes any existing keyframe animations to prevent overlapping conflicts."""
    print("[Animation Engine] Purging old animation data...")
    # Clear animations on meshes
    for obj in bpy.data.objects:
        if obj.animation_data:
            obj.animation_data_clear()
    # Clear animations on cameras and lights
    for cam in bpy.data.cameras:
        if cam.animation_data:
            cam.animation_data_clear()
    for light in bpy.data.lights:
        if light.animation_data:
            light.animation_data_clear()

def setup_turntable_animation(brain_parent, frames=120):
    """Sets up a beautiful, seamless 360-degree turntable loop by rotating the brain assembly."""
    print("[Animation Engine] Setting up Turntable Orbit animation...")
    clear_animation_data()
    init_animation_settings(end_frame=frames)
    
    # Select the brain parent empty
    brain_parent.rotation_mode = 'XYZ'
    brain_parent.rotation_euler = (0.0, 0.0, 0.0)
    
    # Under Blender 5.0+ slotted animation system or legacy, setting keyframe_new_interpolation_type handles this beautifully at keyframe creation time.
    bpy.context.preferences.edit.keyframe_new_interpolation_type = 'LINEAR'
    
    # Insert keyframe at start (Frame 1, Z = 0)
    brain_parent.keyframe_insert(data_path="rotation_euler", index=2, frame=1)
    
    # Insert keyframe at end (Frame 120, Z = 360 degrees in radians)
    brain_parent.rotation_euler = (0.0, 0.0, 2 * math.pi)
    brain_parent.keyframe_insert(data_path="rotation_euler", index=2, frame=frames)
                    
    print("[Animation Engine] Turntable keyframes inserted successfully!")

def setup_explode_animation(brain_parent, frames=120):
    """Animates the anatomical separation of all structures along their coordinate vectors, followed by reassembly."""
    print("[Animation Engine] Setting up Explode & Reassemble Morph animation...")
    clear_animation_data()
    init_animation_settings(end_frame=frames)
    
    # Define hand-tuned 3D clinical translation vectors for each structure
    explode_directions = {
        'Cerebrum_Left': (-0.65, 0.20, 0.0),
        'Cerebrum_Right': (0.65, 0.20, 0.0),
        'Cerebellum': (0.0, -0.25, -0.60),
        'Corpus_Callosum': (0.0, 0.40, 0.0),
        
        'Thalamus_Left': (-0.20, 0.15, 0.08),
        'Thalamus_Right': (0.20, 0.15, 0.08),
        'Interthalamic_Adhesion': (0.0, 0.20, 0.04),
        
        'Caudate_Left': (-0.30, 0.25, 0.04),
        'Caudate_Right': (0.30, 0.25, 0.04),
        'Putamen_Left': (-0.40, 0.12, -0.04),
        'Putamen_Right': (0.40, 0.12, -0.04),
        'Globus_Pallidus_Left': (-0.26, 0.08, -0.04),
        'Globus_Pallidus_Right': (0.26, 0.08, -0.04),
        'Nucleus_Accumbens_Left': (-0.30, -0.04, 0.16),
        'Nucleus_Accumbens_Right': (0.30, -0.04, 0.16),
        
        'Hypothalamus': (0.0, 0.08, 0.16),
        'Infundibulum_Stalk': (0.0, -0.04, 0.20),
        'Pituitary_Gland': (0.0, -0.12, 0.28),
        'Pineal_Gland': (0.0, 0.08, -0.28),
        'Pineal_Stalks': (0.0, 0.06, -0.24),
        
        'Hippocampus_Left': (-0.30, -0.08, 0.12),
        'Hippocampus_Right': (0.30, -0.08, 0.12),
        'Fornix': (0.0, 0.28, -0.04),
        'Amygdala_Left': (-0.34, -0.12, 0.20),
        'Amygdala_Right': (0.34, -0.12, 0.20),
        
        'Midbrain': (0.0, -0.08, 0.12),
        'VTA': (0.0, -0.08, 0.08),
        'Substantia_Nigra_Left': (-0.12, -0.12, 0.04),
        'Substantia_Nigra_Right': (0.12, -0.12, 0.04),
        
        'Pons': (0.0, -0.16, 0.20),
        'Medulla_Oblongata': (0.0, -0.32, 0.12),
        'Spinal_Cord': (0.0, -0.55, -0.08),
        'Spinal_Nerve_Rootlets': (0.0, -0.70, -0.16)
    }
    
    # Animate children locations:
    # Frame 1: Fully merged local location = (0, 0, 0)
    # Frame 40: Fully exploded translated location
    # Frame 80: Remain exploded (hold details)
    # Frame 120: Return home to (0, 0, 0)
    
    # Set preferences to Bezier for fluid morphing motion
    bpy.context.preferences.edit.keyframe_new_interpolation_type = 'BEZIER'
    
    for child in brain_parent.children:
        name = child.name
        if name in explode_directions:
            vector = explode_directions[name]
            child.location = (0.0, 0.0, 0.0)
            
            # Frame 1 keyframe
            child.keyframe_insert(data_path="location", frame=1)
            
            # Frame 40 keyframe
            child.location = vector
            child.keyframe_insert(data_path="location", frame=40)
            
            # Frame 80 keyframe (hold)
            child.keyframe_insert(data_path="location", frame=80)
            
            # Frame 120 keyframe (return home)
            child.location = (0.0, 0.0, 0.0)
            child.keyframe_insert(data_path="location", frame=120)
                            
    print("[Animation Engine] Explode & Reassemble keyframes successfully configured!")

def setup_flythrough_animation(camera, frames=120):
    """Keyframes a cinematic helical camera path that sweeps around the brain and dives deep into the diencephalon."""
    print("[Animation Engine] Setting up Helical subcortical Fly-Through animation...")
    clear_animation_data()
    init_animation_settings(end_frame=frames)
    
    # 1. Add a target empty in the center of the subcortical matrix
    target_loc = (0.0, -0.15, -0.15)
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=target_loc)
    target = bpy.context.active_object
    target.name = "Animation_Camera_Target"
    
    # 2. Add Track-To constraint to camera
    set_active_and_selected(camera)
    constraint = camera.constraints.new(type='TRACK_TO')
    constraint.target = target
    constraint.track_axis = 'TRACK_NEGATIVE_Z'
    constraint.up_axis = 'UP_Y'
    
    # 3. Animate the camera in a descending spiral
    # Radius shrinks from 3.2 to 1.1 (zooming in), height (z) drops from 1.6 to -0.6 (descending through thalami)
    for f in range(1, frames + 1):
        t = (f - 1) / (frames - 1)  # Normalised progress (0.0 to 1.0)
        
        # Helical calculations
        angle = t * 2.0 * math.pi * 1.25  # 1.25 complete orbits
        radius = 3.4 * (1.0 - t * 0.6)    # Spiral inward
        h = 1.6 - t * 2.2                # Descent from above to below
        
        x = radius * math.cos(angle)
        y = radius * math.sin(angle) - 0.2
        z = h
        
        camera.location = (x, y, z)
        camera.keyframe_insert(data_path="location", frame=f)
        
    print("[Animation Engine] Helical fly-through path mapped and keyframed successfully!")

def set_active_and_selected(obj):
    """Guarantees context selection for modifiers and operators."""
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

def configure_render_output(output_dir, animation_name):
    """Sets up the rendering folder paths and format (cinematic EEVEE Next)."""
    scene = bpy.context.scene
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    
    render_folder = os.path.join(output_dir, animation_name)
    if not os.path.exists(render_folder):
        os.makedirs(render_folder)
        
    scene.render.filepath = os.path.join(render_folder, "frame_###")
    print(f"[Animation Engine] Render outputs will be compiled to: {render_folder}")
    return render_folder

def main():
    # 1. Parse manual arguments passed through background script run
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1:]
    else:
        argv = []
        
    parser = argparse.ArgumentParser(description="BioVR Cinematic 3D Animation Generator")
    parser.add_argument('--mode', type=str, default='turntable', choices=['turntable', 'explode', 'flythrough'],
                        help="Choose turntable, explode, or flythrough cinematic path")
    parser.add_argument('--frames', type=int, default=120, help="Total animation frames")
    parser.add_argument('--render', action='store_true', help="Render frame sequences to files")
    
    args = parser.parse_args(argv)
    
    # 2. Open the source project blend file
    blend_path = os.path.join(os.getcwd(), "human_brain.blend")
    if not os.path.exists(blend_path):
        print(f"[ERROR] Source Blend file '{blend_path}' not found! Please run generate_brain.py first.")
        sys.exit(1)
        
    print(f"[Animation Engine] Loading neuroanatomical source model: {blend_path}")
    bpy.ops.wm.open_mainfile(filepath=blend_path)
    
    # 3. Locate crucial handles (Brain Parent and camera)
    brain_parent = bpy.data.objects.get("Human_Brain")
    camera = bpy.data.objects.get("Cinematic_Camera")
    
    if not brain_parent:
        print("[ERROR] 'Human_Brain' parent object was not found in the blend file!")
        sys.exit(1)
    if not camera:
        print("[ERROR] 'Cinematic_Camera' was not found in the blend file!")
        sys.exit(1)
        
    # 4. Generate the requested animation keyframes
    if args.mode == 'turntable':
        setup_turntable_animation(brain_parent, frames=args.frames)
    elif args.mode == 'explode':
        setup_explode_animation(brain_parent, frames=args.frames)
    elif args.mode == 'flythrough':
        setup_flythrough_animation(camera, frames=args.frames)
        
    # Save the keyframed work as a separate file to preserve the static base model
    animated_blend = os.path.join(os.getcwd(), f"human_brain_animated_{args.mode}.blend")
    bpy.ops.wm.save_as_mainfile(filepath=animated_blend)
    print(f"[Animation Engine] Saved keyframed animation model to: {animated_blend}")
    
    # 5. Perform headless rendering if requested
    if args.render:
        output_dir = os.path.join(os.getcwd(), "renders")
        configure_render_output(output_dir, args.mode)
        
        print(f"[Animation Engine] Rendering cinematic H.264 loop frames headlessly...")
        bpy.ops.render.render(animation=True, write_still=True)
        print(f"[Animation Engine] Cinematic render successfully compiled!")
        
if __name__ == "__main__":
    main()
