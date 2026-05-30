import bpy

def test_decimate():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    
    is_left = True
    lat_sign = -1 if is_left else 1
    
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=(lat_sign * 0.08, 0.42, 0.22))
    front = bpy.context.active_object
    front.scale = (0.55, 0.65, 0.5)
    
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=(lat_sign * 0.08, -0.15, 0.35))
    parietal = bpy.context.active_object
    parietal.scale = (0.58, 0.65, 0.52)
    
    bpy.ops.object.select_all(action='DESELECT')
    front.select_set(True)
    parietal.select_set(True)
    bpy.context.view_layer.objects.active = front
    bpy.ops.object.join()
    
    obj = bpy.context.active_object
    
    # Remesh
    remesh = obj.modifiers.new(name="Remesh", type='REMESH')
    remesh.mode = 'VOXEL'
    remesh.voxel_size = 0.035
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    print("Pre-decimate vertex count:", len(obj.data.vertices))
    
    # Decimate
    decimate = obj.modifiers.new(name="Decimate", type='DECIMATE')
    decimate.ratio = 0.12
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    print("Post-decimate vertex count:", len(obj.data.vertices))

if __name__ == "__main__":
    test_decimate()
