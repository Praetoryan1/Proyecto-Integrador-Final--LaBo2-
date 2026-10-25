<script setup>
import { computed, ref } from "vue";
import { solicitar } from "./http";

const props = defineProps({ post: { type: Object, required: true }, csrf: String, disabled: Boolean });
const emit = defineEmits(["actualizado", "ocupado"]);
const ocupado = ref(false);
const error = ref("");
const mensaje = ref("");
const archivo = ref(null);
const entrada = ref(null);
const alt = ref("");
const creditos = ref("");
const url = ref("");
const modo = ref("archivo");
const editando = ref(null);
const textoEdicion = ref("");
const creditosEdicion = ref("");
const bloqueado = computed(() => props.disabled || ocupado.value);
const tipos = ["Imagen", "GIF", "Video MP4", "YouTube"];
const base = computed(() => `/panel-json/publicaciones/${props.post.id}/recursos`);

async function ejecutar(path, method, data) {
    if (bloqueado.value) return false;
    ocupado.value = true;
    emit("ocupado", true);
    error.value = "";
    mensaje.value = "";
    try {
        const result = await solicitar(base.value + path, { method, data, csrf: props.csrf });
        emit("actualizado", result);
        mensaje.value = "Recursos guardados.";
        return true;
    } catch (e) {
        error.value = e.message;
        return false;
    } finally {
        ocupado.value = false;
        emit("ocupado", false);
    }
}
async function agregar() {
    let data;
    if (modo.value === "archivo") {
        if (!archivo.value) return;
        data = new FormData();
        data.append("archivo", archivo.value);
        data.append("version", props.post.version);
        data.append("textoAlternativo", alt.value);
        data.append("creditos", creditos.value);
    } else {
        data = { version: props.post.version, url: url.value, textoAlternativo: alt.value, creditos: creditos.value };
    }
    if (await ejecutar(modo.value === "archivo" ? "/archivo" : "/youtube", "POST", data)) {
        alt.value = ""; creditos.value = ""; url.value = ""; archivo.value = null;
        if (entrada.value) entrada.value.value = "";
    }
}
function editar(recurso) {
    editando.value = recurso.id;
    textoEdicion.value = recurso.textoAlternativo;
    creditosEdicion.value = recurso.creditos;
}
async function guardarDescripcion() {
    if (await ejecutar(`/${editando.value}`, "PUT", { version: props.post.version, textoAlternativo: textoEdicion.value, creditos: creditosEdicion.value })) editando.value = null;
}
async function eliminar(recurso) {
    if (!window.confirm(`¿Eliminar el recurso “${recurso.textoAlternativo}”?`)) return;
    if (await ejecutar(`/${recurso.id}`, "DELETE", { version: props.post.version })) editando.value = null;
}
async function mover(index, delta) {
    const ids = props.post.recursos.map(r => r.id);
    [ids[index], ids[index + delta]] = [ids[index + delta], ids[index]];
    await ejecutar("/orden", "PUT", { version: props.post.version, recursoIds: ids });
}
</script>

<template>
    <section class="recursos-editor" aria-labelledby="recursos-titulo" :aria-busy="ocupado">
        <h3 id="recursos-titulo">Medios de la publicación <span class="accent">{{ post.recursos.length }}/10</span></h3>
        <p class="field-help">El primer archivo es la portada. Cada cambio de medios se guarda por separado.</p>
        <p v-if="disabled && !ocupado" class="notice">Guardá primero los cambios del texto para administrar sus medios.</p>
        <p v-if="ocupado" class="notice" role="status">Procesando el recurso… Esperá antes de salir de esta página.</p>
        <p v-if="error" class="notice error" role="alert">{{ error }} Si la versión cambió, cerrá y volvé a abrir la publicación.</p>
        <p v-if="mensaje" class="notice success" role="status">{{ mensaje }}</p>
        <ol class="resource-list">
            <li v-for="(recurso, index) in post.recursos" :key="recurso.id" class="resource-item">
                <img v-if="recurso.vistaPreviaUrl" :src="recurso.vistaPreviaUrl" :alt="recurso.textoAlternativo" loading="lazy" />
                <span v-else class="resource-youtube" aria-hidden="true">▶ YouTube</span>
                <div class="resource-info">
                    <strong>{{ index + 1 }}. {{ tipos[recurso.tipo] }}</strong>
                    <p>{{ recurso.textoAlternativo }}</p>
                    <small v-if="recurso.creditos">{{ recurso.creditos }}</small>
                    <div class="resource-actions">
                        <button type="button" class="quiet-button" :disabled="bloqueado || index === 0" :aria-label="`Subir recurso ${index + 1}`" @click="mover(index, -1)">↑</button>
                        <button type="button" class="quiet-button" :disabled="bloqueado || index === post.recursos.length - 1" :aria-label="`Bajar recurso ${index + 1}`" @click="mover(index, 1)">↓</button>
                        <button type="button" class="quiet-button" :disabled="bloqueado" @click="editar(recurso)">Descripción</button>
                        <button type="button" class="quiet-button danger-link" :disabled="bloqueado" @click="eliminar(recurso)">Quitar</button>
                    </div>
                </div>
            </li>
        </ol>
        <form v-if="editando" class="resource-form" @submit.prevent="guardarDescripcion">
            <fieldset :disabled="bloqueado">
                <label for="recurso-editar-alt">Editar descripción del recurso</label>
                <textarea id="recurso-editar-alt" v-model="textoEdicion" class="form-control" required maxlength="500" />
                <label for="recurso-editar-creditos">Editar créditos</label>
                <input id="recurso-editar-creditos" v-model="creditosEdicion" class="form-control" maxlength="300" />
                <button class="btn btn-lime">Guardar descripción</button>
                <button type="button" class="quiet-button" @click="editando = null">Cancelar edición</button>
            </fieldset>
        </form>
        <form v-if="post.recursos.length < 10" class="resource-form" @submit.prevent="agregar">
            <fieldset :disabled="bloqueado">
                <label for="recurso-modo">Agregar medio</label>
                <select id="recurso-modo" v-model="modo" class="form-select">
                    <option value="archivo">Archivo de mi equipo</option><option value="youtube">Enlace a YouTube</option>
                </select>
                <template v-if="modo === 'archivo'">
                    <label for="recurso-archivo">Archivo</label>
                    <input id="recurso-archivo" ref="entrada" class="form-control" type="file" required accept=".jpg,.jpeg,.png,.webp,.gif,.mp4" @change="archivo = $event.target.files[0]" />
                    <p class="field-help">JPG/PNG/WebP: 10 MB y 16 MP. GIF: 15 MB, 30 s y hasta 300 cuadros. MP4 H.264/AAC: 50 MB, 60 s y Full HD. Cuota: 500 MB por cuenta. Solo subí contenido que tengas derecho a compartir.</p>
                </template>
                <template v-else>
                    <label for="recurso-url">Enlace HTTPS de YouTube</label>
                    <input id="recurso-url" v-model="url" class="form-control" type="url" required maxlength="500" placeholder="https://www.youtube.com/watch?v=…" />
                    <p class="field-help">La privacidad de Trazo no cambia los permisos de YouTube. El visitante decide si carga el reproductor externo.</p>
                </template>
                <label for="recurso-alt">Descripción accesible del recurso</label>
                <textarea id="recurso-alt" v-model="alt" class="form-control" required maxlength="500" rows="2" placeholder="Describí lo que se ve o el contenido del video." />
                <label for="recurso-creditos">Créditos (opcional)</label>
                <input id="recurso-creditos" v-model="creditos" class="form-control" maxlength="300" placeholder="Autoría, colaboradores o licencia" />
                <button type="submit" class="btn btn-lime">{{ ocupado ? "Procesando…" : "Agregar recurso +" }}</button>
            </fieldset>
        </form>
    </section>
</template>
