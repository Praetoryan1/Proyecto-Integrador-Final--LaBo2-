<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from "vue";
import RecursosEditor from "./RecursosEditor.vue";
import SelectorEtiquetas from "./SelectorEtiquetas.vue";
import { solicitar } from "./http";

const props = defineProps({ csrf: { type: String, required: true } });
const estados = ["Borrador", "Publicada", "Archivada"];
const visibilidades = ["Pública", "Usuarios registrados", "Privada"];
const listado = ref(null);
const cargando = ref(false);
const guardando = ref(false);
const abriendo = ref(false);
const mediosOcupados = ref(false);
const busqueda = ref("");
const filtroEstado = ref("");
const consulta = ref({ q: "", estado: "" });
const errorLista = ref("");
const errorEditor = ref("");
const mensaje = ref("");
const conflicto = ref(false);
const form = ref(null);
const original = ref("");
const tituloInput = ref(null);
const errorRef = ref(null);
const sucio = computed(
    () => form.value !== null && JSON.stringify(form.value) !== original.value,
);
let listRequest;
let listSequence = 0;
const vacio = () => ({
    id: null,
    titulo: "",
    texto: "",
    tipo: 1,
    estadoEditorial: 0,
    visibilidad: 0,
    estadoModeracion: 0,
    version: "",
    etiquetas: [],
    recursos: [],
});
const fecha = (value) =>
    new Intl.DateTimeFormat("es-AR", {
        dateStyle: "short",
        timeStyle: "short",
    }).format(new Date(value));

async function cargar(numero = 1) {
    const current = ++listSequence;
    listRequest?.abort();
    listRequest = new AbortController();
    cargando.value = true;
    errorLista.value = "";
    const params = new URLSearchParams({ q: consulta.value.q, pagina: numero });
    if (consulta.value.estado !== "")
        params.set("estado", consulta.value.estado);
    try {
        const page = await solicitar(`/panel-json/publicaciones?${params}`, {
            signal: listRequest.signal,
        });
        if (current === listSequence) listado.value = page;
    } catch (e) {
        if (e.name !== "AbortError" && current === listSequence)
            errorLista.value = e.message;
    } finally {
        if (current === listSequence) cargando.value = false;
    }
}
function filtrar() {
    consulta.value = { q: busqueda.value, estado: filtroEstado.value };
    cargar();
}
function confirmarSalida() {
    return (
        !mediosOcupados.value && (!sucio.value ||
        window.confirm("Tenés cambios sin guardar. ¿Querés descartarlos?"))
    );
}
async function mostrar(model) {
    form.value = model;
    original.value = JSON.stringify(model);
    errorEditor.value = "";
    conflicto.value = false;
    await nextTick();
    tituloInput.value?.focus();
}
function nuevo() {
    if (confirmarSalida()) {
        mensaje.value = "";
        mostrar(vacio());
    }
}
async function abrir(id) {
    if (guardando.value || abriendo.value || mediosOcupados.value || !confirmarSalida()) return;
    abriendo.value = true;
    errorLista.value = "";
    try {
        await mostrar(await solicitar(`/panel-json/publicaciones/${id}`));
    } catch (e) {
        errorLista.value = e.message;
    } finally {
        abriendo.value = false;
    }
}
function cerrar() {
    if (confirmarSalida()) form.value = null;
}
async function guardar() {
    if (guardando.value || mediosOcupados.value) return;
    guardando.value = true;
    errorEditor.value = "";
    conflicto.value = false;
    mensaje.value = "";
    const data = {
        titulo: form.value.titulo,
        texto: form.value.texto,
        tipo: form.value.tipo,
        estadoEditorial: form.value.estadoEditorial,
        visibilidad: form.value.visibilidad,
        etiquetaIds: form.value.etiquetas.map((e) => e.id),
        version: form.value.version,
    };
    const id = form.value.id;
    try {
        const result = await solicitar(
            `/panel-json/publicaciones${id ? `/${id}` : ""}`,
            { method: id ? "PUT" : "POST", csrf: props.csrf, data },
        );
        await mostrar(result);
        mensaje.value =
            result.estadoEditorial === 1
                ? "Publicación guardada. Su acceso respeta la visibilidad elegida y la de tu perfil."
                : `${estados[result.estadoEditorial]} guardado correctamente.`;
        await cargar(id ? listado.value?.numero || 1 : 1);
    } catch (e) {
        errorEditor.value = e.message;
        conflicto.value = e.status === 409;
        await nextTick();
        errorRef.value?.focus();
    } finally {
        guardando.value = false;
    }
}
async function eliminar(item) {
    if (guardando.value || abriendo.value || mediosOcupados.value) return;
    if (form.value?.id === item.id && !confirmarSalida()) return;
    if (
        !window.confirm(
            `¿Eliminar “${item.titulo}”? Esta acción es permanente. Podés archivarla desde el editor si preferís conservarla.`,
        )
    )
        return;
    guardando.value = true;
    mensaje.value = "";
    errorLista.value = "";
    try {
        await solicitar(`/panel-json/publicaciones/${item.id}`, {
            method: "DELETE",
            csrf: props.csrf,
            data: { version: item.version },
        });
        if (form.value?.id === item.id) form.value = null;
        mensaje.value = "Publicación eliminada.";
        await cargar(listado.value?.numero || 1);
    } catch (e) {
        errorLista.value = e.message;
    } finally {
        guardando.value = false;
    }
}
function actualizarMedios(model) {
    form.value = model; original.value = JSON.stringify(model);
    cargar(listado.value?.numero || 1);
}
function salirPagina(event) {
    if (sucio.value || mediosOcupados.value) {
        event.preventDefault();
        event.returnValue = "";
    }
}
onMounted(async () => {
    window.addEventListener("beforeunload", salirPagina);
    await cargar();
    const id = new URLSearchParams(window.location.search).get("editar");
    if (id && /^\d+$/.test(id)) await abrir(id);
});
onBeforeUnmount(() => {
    listRequest?.abort();
    window.removeEventListener("beforeunload", salirPagina);
});
</script>

<template>
    <div class="publicaciones-workspace">
        <div v-if="mensaje" class="notice success" role="status">
            {{ mensaje }}
        </div>
        <div v-if="errorLista" class="notice error" role="alert">
            {{ errorLista }}
            <button
                type="button"
                class="quiet-button"
                :disabled="cargando"
                @click="cargar(listado?.numero || 1)"
            >
                Actualizar listado
            </button>
        </div>
        <div class="publicaciones-columns" :class="{ 'with-editor': form }">
            <section
                class="publicaciones-list"
                aria-labelledby="mis-publicaciones-titulo"
                :aria-busy="cargando"
            >
                <div class="workspace-heading">
                    <div>
                        <span class="eyebrow">TU ARCHIVO CREATIVO</span>
                        <h2 id="mis-publicaciones-titulo">
                            Mis publicaciones<span class="accent">.</span>
                        </h2>
                    </div>
                    <button
                        type="button"
                        class="btn btn-lime"
                        :disabled="guardando || abriendo || mediosOcupados"
                        @click="nuevo"
                    >
                        Nueva publicación +
                    </button>
                </div>
                <form class="post-filters" @submit.prevent="filtrar">
                    <div>
                        <label for="buscar-publicacion">Buscar por título</label
                        ><input
                            id="buscar-publicacion"
                            v-model="busqueda"
                            class="form-control"
                            maxlength="100"
                            placeholder="Una idea, un proceso…"
                        />
                    </div>
                    <div>
                        <label for="filtrar-estado">Estado</label
                        ><select
                            id="filtrar-estado"
                            v-model="filtroEstado"
                            class="form-select"
                        >
                            <option value="">Todos</option>
                            <option
                                v-for="(estado, index) in estados"
                                :key="index"
                                :value="String(index)"
                            >
                                {{ estado }}
                            </option>
                        </select>
                    </div>
                    <button class="btn btn-outline-light" :disabled="cargando">
                        Buscar
                    </button>
                </form>
                <p v-if="cargando" role="status" class="muted">
                    Cargando publicaciones…
                </p>
                <div
                    v-if="listado?.total === 0 && !cargando"
                    class="empty-posts"
                >
                    <span class="empty-glyph" aria-hidden="true">✳</span>
                    <h3>
                        {{
                            consulta.q || consulta.estado !== ""
                                ? "Sin coincidencias"
                                : "Tu próxima idea empieza acá"
                        }}
                    </h3>
                    <p>
                        {{
                            consulta.q || consulta.estado !== ""
                                ? "Probá otro título o estado."
                                : "Creá tu primera actualización. También podés guardarla como borrador."
                        }}
                    </p>
                </div>
                <ul v-if="listado && !cargando" class="post-rows">
                    <li
                        v-for="item in listado.items"
                        :key="item.id"
                        class="post-row"
                        :class="{ selected: form?.id === item.id }"
                    >
                        <div class="post-row-title">
                            <span class="post-kind">{{
                                item.tipo === 0 ? "OBRA" : "ACTUALIZACIÓN"
                            }}</span>
                            <h3>{{ item.titulo }}</h3>
                            <p>Actualizada {{ fecha(item.actualizadaUtc) }}</p>
                        </div>
                        <div class="post-status">
                            <span
                                class="status-pill"
                                :class="`status-${item.estadoEditorial}`"
                                >{{ estados[item.estadoEditorial] }}</span
                            ><span>{{ visibilidades[item.visibilidad] }}</span
                            ><span
                                v-if="item.estadoModeracion === 1"
                                class="moderation-label"
                                >Oculta por moderación</span
                            >
                        </div>
                        <div class="post-actions">
                            <button
                                type="button"
                                class="quiet-button"
                                :disabled="guardando || abriendo || mediosOcupados"
                                :aria-label="`Editar ${item.titulo}`"
                                @click="abrir(item.id)"
                            >
                                Editar ↗</button
                            ><a
                                :href="`/publicaciones/${item.id}`"
                                target="_blank"
                                rel="noopener"
                                :aria-label="`Ver ${item.titulo} en otra pestaña`"
                                >Ver</a
                            ><button
                                type="button"
                                class="quiet-button danger-link"
                                :disabled="guardando || abriendo || mediosOcupados"
                                :aria-label="`Eliminar ${item.titulo}`"
                                @click="eliminar(item)"
                            >
                                Eliminar
                            </button>
                        </div>
                    </li>
                </ul>
                <nav
                    v-if="listado"
                    class="pagination-bar"
                    aria-label="Páginas de mis publicaciones"
                >
                    <span
                        >{{ listado.total }} resultados · Página
                        {{ listado.numero }} de {{ listado.paginas }}</span
                    >
                    <div class="post-pagination">
                        <button
                            type="button"
                            class="btn btn-outline-light"
                            :disabled="cargando || !listado.anterior"
                            @click="cargar(listado.numero - 1)"
                        >
                            ← Anterior</button
                        ><button
                            type="button"
                            class="btn btn-outline-light"
                            :disabled="cargando || !listado.siguiente"
                            @click="cargar(listado.numero + 1)"
                        >
                            Siguiente →
                        </button>
                    </div>
                </nav>
            </section>
            <section
                v-if="form"
                class="post-editor"
                aria-labelledby="editor-titulo"
            >
                <div class="workspace-heading">
                    <div>
                        <span class="eyebrow">{{
                            form.id ? "VOLVÉ SOBRE TU IDEA" : "UNA NUEVA MIRADA"
                        }}</span>
                        <h2 id="editor-titulo">
                            {{
                                form.id
                                    ? "Editar publicación"
                                    : "Nueva publicación"
                            }}
                        </h2>
                    </div>
                    <button
                        type="button"
                        class="quiet-button"
                        :disabled="guardando || mediosOcupados"
                        @click="cerrar"
                        aria-label="Cerrar editor"
                    >
                        ×
                    </button>
                </div>
                <p class="field-help">
                    {{
                        sucio
                            ? "Tenés cambios sin guardar."
                            : "Los cambios se guardan al confirmar el formulario."
                    }}
                </p>
                <div
                    v-if="errorEditor"
                    ref="errorRef"
                    class="notice error"
                    role="alert"
                    tabindex="-1"
                >
                    {{ errorEditor
                    }}<button
                        v-if="conflicto"
                        type="button"
                        class="quiet-button"
                        :disabled="guardando || mediosOcupados"
                        @click="abrir(form.id)"
                    >
                        Cargar versión actual
                    </button>
                </div>
                <p v-if="form.estadoModeracion === 1" class="notice">
                    Esta publicación está oculta por moderación. Editarla no
                    restablece su difusión.
                </p>
                <form @submit.prevent="guardar">
                    <fieldset :disabled="guardando || abriendo || mediosOcupados">
                        <label for="post-titulo">Título</label
                        ><input
                            id="post-titulo"
                            ref="tituloInput"
                            v-model="form.titulo"
                            class="form-control"
                            maxlength="160"
                            required
                            placeholder="Dale un nombre a tu idea"
                        />
                        <label for="post-texto">Texto</label
                        ><textarea
                            id="post-texto"
                            v-model="form.texto"
                            class="form-control post-textarea"
                            maxlength="12000"
                            required
                            rows="8"
                            placeholder="Contá el proceso, el contexto o lo que estás explorando."
                        />
                        <p class="field-help text-end">
                            {{ form.texto.length }} / 12000
                        </p>
                        <div class="post-options">
                            <div>
                                <label for="post-tipo">Tipo</label
                                ><select
                                    id="post-tipo"
                                    v-model="form.tipo"
                                    class="form-select"
                                >
                                    <option :value="1">
                                        Actualización de texto
                                    </option>
                                    <option :value="0">Obra</option>
                                </select>
                            </div>
                            <div>
                                <label for="post-estado">Estado editorial</label
                                ><select
                                    id="post-estado"
                                    v-model="form.estadoEditorial"
                                    class="form-select"
                                >
                                    <option
                                        v-for="(estado, index) in estados"
                                        :key="index"
                                        :value="index"
                                    >
                                        {{ estado }}
                                    </option>
                                </select>
                            </div>
                        </div>
                        <p v-if="form.tipo === 0" class="notice">
                            Guardá el borrador y agregá sus medios debajo.
                            Podés preparar el borrador de tu obra; para
                            publicarla necesitarás un recurso listo.
                        </p>
                        <label for="post-visibilidad">Visibilidad</label
                        ><select
                            id="post-visibilidad"
                            v-model="form.visibilidad"
                            class="form-select"
                        >
                            <option
                                v-for="(visibilidad, index) in visibilidades"
                                :key="index"
                                :value="index"
                            >
                                {{ visibilidad }}
                            </option>
                        </select>
                        <p class="field-help">
                            Siempre se aplica también la privacidad de tu
                            perfil. Los borradores y las publicaciones
                            archivadas solo los ves vos.
                        </p>
                        <SelectorEtiquetas
                            :key="form.id || 'nueva'"
                            v-model="form.etiquetas"
                            :disabled="guardando || abriendo || mediosOcupados"
                        />
                        <div class="editor-footer">
                            <button class="btn btn-lime" type="submit">
                                {{
                                    guardando
                                        ? "Guardando…"
                                        : form.estadoEditorial === 1
                                          ? "Guardar publicación →"
                                          : form.estadoEditorial === 0
                                            ? "Guardar borrador →"
                                            : "Guardar como archivada →"
                                }}</button
                            ><button
                                type="button"
                                class="quiet-button"
                                @click="cerrar"
                            >
                                Cancelar
                            </button>
                        </div>
                    </fieldset>
                </form>
                <RecursosEditor v-if="form.id" :key="form.id" :post="form" :csrf="csrf" :disabled="sucio || guardando || abriendo" @ocupado="mediosOcupados = $event" @actualizado="actualizarMedios" />
                <p v-else class="notice">Guardá primero el borrador para agregar archivos o YouTube.</p>
            </section>
        </div>
    </div>
</template>
