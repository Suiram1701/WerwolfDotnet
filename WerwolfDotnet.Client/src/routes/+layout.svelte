<script lang="ts">
    import { onMount } from "svelte";
    
    const colorKey: string = "colorTheme";
    
    interface colorMode {
        name: string,
        value: "light" | "dark" | "auto",
        icon: string
    }
    const colorModes: colorMode[] = [
        { name: "Hell", value: "light", icon: "light_mode"},
        { name: "Dunkel", value: "dark", icon: "dark_mode"},
        { name: "Automatisch", value: "auto", icon: "contrast"}
    ];
    
    let currentColor: colorMode = $state(colorModes[2]);
    onMount(() => {
        const savedColor: string | null = localStorage.getItem(colorKey);
        if (savedColor !== null)
            currentColor = colorModes.find(c => c.value === savedColor);
    });

    $effect(() => {
        localStorage.setItem(colorKey, currentColor.value);
        
        let isDarkMode = false;
        if (currentColor.value === "auto")
            isDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;
        else
            isDarkMode = currentColor.value === "dark";

        const attrValue = isDarkMode ? "dark" : "light";
        document.getElementsByTagName("html")[0].setAttribute("data-bs-theme", attrValue);
    });
    
    export const ssr = false;
</script>

<div class="container my-3">
    <slot/>
</div>

<div class="dropdown position-fixed bottom-0 end-0 mb-3 me-3">
    <button class="btn btn-primary py-2 dropdown-toggle d-flex align-items-center" type="button" data-bs-toggle="dropdown">
        <span class="material-symbols-outlined">{currentColor.icon}</span>
    </button>
    <ul class="dropdown-menu dropdown-menu-end shadow">
        {#each colorModes as mode}
            <li>
                <button type="button" class="dropdown-item d-flex justify-content-between align-items-center" onclick={() => currentColor = mode}>
                    <div class="d-flex align-items-center">
                        <span class="material-symbols-outlined me-1">{mode.icon}</span>
                        {mode.name}
                    </div>
                    {#if currentColor.value === mode.value}
                        <span class="material-symbols-outlined">check</span>
                    {/if}
                </button>
            </li>
        {/each}
    </ul>
</div>
