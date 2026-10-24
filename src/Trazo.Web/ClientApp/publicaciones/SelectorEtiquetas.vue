<script setup>
import { onBeforeUnmount, ref, watch } from "vue";
import { solicitar } from "./http";

const props = defineProps({
    modelValue: { type: Array, required: true },
    disabled: Boolean,
});
const emit = defineEmits(["update:modelValue"]);
const termino = ref("");
const abierto = ref(false);
const cargando = ref(false);
const error = ref("");
const pagina = ref(null);
let timer;
let request;
let sequence = 0;

async function buscar(numero = 1) {
    const current = ++sequence;
    request?.abort();
    request = new AbortController();
    cargando.value = true;
    error.value = "";
    try {
        const result = await solicitar(
            `/panel-json/etiquetas?${new URLSearchParams({ q: termino.value, pagina: numero })}`,
            { signal: request.signal },
        );
        if (current === sequence) pagina.value = result;
    } catch (e) {
        if (e.name !== "AbortError" && current === sequence)
            error.value = e.message;
    } finally {
        if (current === sequence) cargando.value = false;
    }
}
watch(termino, () => {
    clearTimeout(timer);
    request?.abort();
    sequence++;
    pagina.value = null;
    if (abierto.value) {
        cargando.value = true;
        timer = setTimeout(() => buscar(), 300);
    }
});
function abrir() {
    abierto.value = true;
    buscar();
}
function cerrar() {
    abierto.value = false;
    clearTimeout(timer);
    request?.abort();
    sequence++;
    cargando.value = false;
}
function agregar(tag) {
    if (
        props.modelValue.length >= 8 ||
        props.modelValue.some((e) => e.id === tag.id)
    )
        return;
    emit("update:modelValue", [...props.modelValue, tag]);
}
function quitar(id) {
    emit(
        "update:modelValue",
        props.modelValue.filter((e) => e.id !== id),
    );
}
onBeforeUnmount(cerrar);
</script>

<template>
    <fieldset class="tag-picker" :disabled="disabled">
        <legend>
            Etiquetas <span class="muted">{{ modelValue.length }}/8</span>
        </legend>
        <p class="field-help">
            Elegí temas para que encuentren tu trabajo. Hasta 8 etiquetas.
        </p>
        <ul
            v-if="modelValue.length"
            class="selected-tags"
            aria-label="Etiquetas seleccionadas"
        >
            <li v-for="tag in modelValue" :key="tag.id">
                <span
                    >#{{ tag.nombre
                    }}{{ tag.activa ? "" : " · inactiva" }}</span
                >
                <button
                    type="button"
                    :aria-label="`Quitar etiqueta ${tag.nombre}`"
                    @click="quitar(tag.id)"
                >
                    ×
                </button>
            </li>
        </ul>
        <button
            v-if="!abierto"
            type="button"
            class="btn btn-outline-light"
            @click="abrir"
        >
            Buscar etiquetas +
        </button>
        <div v-else class="tag-search-panel">
            <label for="buscar-etiqueta">Buscar etiqueta</label>
            <input
                id="buscar-etiqueta"
                v-model="termino"
                class="form-control"
                maxlength="40"
                autocomplete="off"
                placeholder="Por ejemplo, animación"
                @keydown.esc.prevent="cerrar"
            />
            <p v-if="cargando" role="status" class="field-help">
                Buscando etiquetas…
            </p>
            <p v-if="error" role="alert" class="notice error">{{ error }}</p>
            <ul
                v-if="!cargando && pagina"
                class="tag-results"
                aria-label="Resultados de etiquetas"
            >
                <li v-for="tag in pagina.items" :key="tag.id">
                    <button
                        type="button"
                        class="tag-result"
                        :disabled="
                            modelValue.some((e) => e.id === tag.id) ||
                            modelValue.length >= 8
                        "
                        @click="agregar(tag)"
                    >
                        <span>#{{ tag.nombre }}</span
                        ><span>{{
                            modelValue.some((e) => e.id === tag.id)
                                ? "Seleccionada"
                                : "Agregar +"
                        }}</span>
                    </button>
                </li>
            </ul>
            <p v-if="!cargando && pagina?.total === 0" class="field-help">
                No hay etiquetas que coincidan.
            </p>
            <div class="tag-pager">
                <template v-if="pagina && pagina.paginas > 1">
                    <button
                        type="button"
                        class="quiet-button"
                        :disabled="cargando || !pagina.anterior"
                        @click="buscar(pagina.numero - 1)"
                    >
                        ← Anteriores
                    </button>
                    <span>{{ pagina.numero }} / {{ pagina.paginas }}</span>
                    <button
                        type="button"
                        class="quiet-button"
                        :disabled="cargando || !pagina.siguiente"
                        @click="buscar(pagina.numero + 1)"
                    >
                        Siguientes →
                    </button>
                </template>
                <button type="button" class="quiet-button" @click="cerrar">
                    Cerrar búsqueda
                </button>
            </div>
        </div>
    </fieldset>
</template>
