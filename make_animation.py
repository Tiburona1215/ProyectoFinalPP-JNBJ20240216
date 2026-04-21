import numpy as np
import matplotlib.pyplot as plt
import matplotlib.animation as animation
import os

N            = 1000   
FRAMES_STEP  = 7      
CARPETA_SEQ  = "sequential/snapshots_seq"
CARPETA_PAR  = "parallel/snapshots_par"
SALIDA_GIF   = "brote_sir.gif"
FPS          = 8      

# Colores para cada estado:
# S=azul, I=rojo, R=verde, D=gris
PALETA = np.array([
    [59,  130, 246],   # S → azul
    [239, 68,  68 ],   # I → rojo
    [34,  197, 94 ],   # R → verde
    [0, 0, 0],         # D → negro
], dtype=np.uint8)

def cargar_snapshots(carpeta):
    archivos = sorted([
        f for f in os.listdir(carpeta) if f.endswith(".bin")
    ])
    print(f"  Encontrados {len(archivos)} snapshots en '{carpeta}'")
    return archivos

def bin_a_imagen(ruta):
    datos = np.frombuffer(open(ruta, "rb").read(), dtype=np.uint8)
    datos = datos.reshape(N, N)

    imagen = PALETA[datos]   
    return imagen, datos

def main():
    print("Cargando snapshots...")
    archivos_seq = cargar_snapshots(CARPETA_SEQ)
    archivos_par = cargar_snapshots(CARPETA_PAR)

    num_frames = min(len(archivos_seq), len(archivos_par))
    print(f"  Generando {num_frames} frames...")

    fig, (ax1, ax2) = plt.subplots(
        1, 2,
        figsize=(12, 6),
        dpi=80   
    )
    fig.patch.set_facecolor("#111827") 

    for ax in (ax1, ax2):
        ax.axis("off")

    img0_seq, datos_seq = bin_a_imagen(f"{CARPETA_SEQ}/{archivos_seq[0]}")
    img0_par, datos_par = bin_a_imagen(f"{CARPETA_PAR}/{archivos_par[0]}")

    im1 = ax1.imshow(img0_seq, interpolation="nearest")
    im2 = ax2.imshow(img0_par, interpolation="nearest")

    titulo1 = ax1.set_title("Secuencial — Día 0",
                             color="white", fontsize=13, pad=8)
    titulo2 = ax2.set_title("Paralelo — Día 0",
                             color="white", fontsize=13, pad=8)

    from matplotlib.patches import Patch
    leyenda = [
        Patch(facecolor="#3B82F6", label="Susceptible"),
        Patch(facecolor="#EF4444", label="Infectado"),
        Patch(facecolor="#22C55E", label="Recuperado"),
        Patch(facecolor="#000000", label="Muerto"),
    ]
    fig.legend(
        handles=leyenda,
        loc="lower center",
        ncol=4,
        frameon=False,
        labelcolor="white",
        fontsize=11,
        bbox_to_anchor=(0.5, 0.01)
    )

    plt.tight_layout(rect=[0, 0.06, 1, 1])

    def actualizar(frame):
        dia = frame * FRAMES_STEP

        img_seq, datos_seq = bin_a_imagen(f"{CARPETA_SEQ}/{archivos_seq[frame]}")
        img_par, datos_par = bin_a_imagen(f"{CARPETA_PAR}/{archivos_par[frame]}")

        im1.set_data(img_seq)
        im2.set_data(img_par)

        s1 = np.sum(datos_seq == 0)
        i1 = np.sum(datos_seq == 1)
        r1 = np.sum(datos_seq == 2)

        s2 = np.sum(datos_par == 0)
        i2 = np.sum(datos_par == 1)
        r2 = np.sum(datos_par == 2)

        titulo1.set_text(f"Secuencial — Día {dia}  |  I={i1:,}")
        titulo2.set_text(f"Paralelo — Día {dia}  |  I={i2:,}")

        if frame % 10 == 0:
            print(f"  Frame {frame}/{num_frames} (día {dia})...")

        return im1, im2, titulo1, titulo2

    print("Generando GIF (puede tardar 1-2 minutos)...")
    ani = animation.FuncAnimation(
        fig,
        actualizar,
        frames=num_frames,
        interval=1000 // FPS, 
        blit=True
    )

    ani.save(SALIDA_GIF, writer="pillow", fps=FPS)
    print(f"\nListo. GIF guardado en: {SALIDA_GIF}")
    print(f"Tamaño aproximado: {os.path.getsize(SALIDA_GIF) // 1024} KB")

if __name__ == "__main__":
    main()