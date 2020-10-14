<CsoundSynthesizer>
<CsOptions> 
-n -d 
</CsOptions>
<CsInstruments>
sr = 44100
ksmps = 32 
nchnls = 2
0dbfs = 1
instr 1 
    aSig oscil 0.5, 440
    outs aSig, aSig
endin

</CsInstruments>
<CsScore>
</CsScore>
</CsoundSynthesizer>
